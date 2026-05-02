using Application.Core;
using Application.DTOs.Recipes;
using Application.Interfaces;
using Application.Validators;
using Domain;
using Domain.Repositories;
using FluentValidation;
using MediatR;

namespace Application.Commands.Recipes
{
  public class Create
  {
    public class Command : IRequest<Result<Guid>>
    {
      public RecipeUpsertDto Recipe { get; set; }
    }

    public class CommandValidator : AbstractValidator<Command>
    {
      public CommandValidator()
      {
        RuleFor(x => x.Recipe).SetValidator(new RecipeUpsertValidator());
      }
    }

    public class Handler : IRequestHandler<Command, Result<Guid>>
    {
      private readonly IRecipeRepository _recipes;
      private readonly IUnitOfWork _unitOfWork;
      private readonly IUserAccessor _userAccessor;

      public Handler(IRecipeRepository recipes, IUnitOfWork unitOfWork, IUserAccessor userAccessor)
      {
        _recipes = recipes;
        _unitOfWork = unitOfWork;
        _userAccessor = userAccessor;
      }

      public async Task<Result<Guid>> Handle(Command request, CancellationToken cancellationToken)
      {
        var ownerId = _userAccessor.GetUserId();
        if (string.IsNullOrEmpty(ownerId))
          return Result<Guid>.Failure("User not found");

        var now = DateTime.UtcNow;
        var recipe = new Recipe
        {
          Id = Guid.NewGuid(),
          OwnerId = ownerId,
          Title = request.Recipe.Title.Trim(),
          Description = request.Recipe.Description?.Trim(),
          SourceUrl = string.IsNullOrWhiteSpace(request.Recipe.SourceUrl) ? null : request.Recipe.SourceUrl.Trim(),
          PrepMinutes = request.Recipe.PrepMinutes,
          CookMinutes = request.Recipe.CookMinutes,
          TotalMinutes = request.Recipe.TotalMinutes,
          Servings = request.Recipe.Servings,
          CreatedUtc = now,
          UpdatedUtc = now
        };

        foreach (var ing in request.Recipe.Ingredients.OrderBy(i => i.SortOrder))
        {
          recipe.Ingredients.Add(new RecipeIngredient
          {
            Id = Guid.NewGuid(),
            SortOrder = ing.SortOrder,
            Name = ing.Name.Trim(),
            Quantity = ing.Quantity,
            Unit = string.IsNullOrWhiteSpace(ing.Unit) ? null : ing.Unit.Trim(),
            Note = string.IsNullOrWhiteSpace(ing.Note) ? null : ing.Note.Trim()
          });
        }

        foreach (var step in request.Recipe.Steps.OrderBy(s => s.SortOrder))
        {
          recipe.Steps.Add(new RecipeStep
          {
            Id = Guid.NewGuid(),
            SortOrder = step.SortOrder,
            Instruction = step.Instruction.Trim()
          });
        }

        foreach (var tag in request.Recipe.Tags.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
          recipe.Tags.Add(new RecipeTag
          {
            Id = Guid.NewGuid(),
            TagName = tag.Trim()
          });
        }

        _recipes.Add(recipe);

        var ok = await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;
        if (!ok) return Result<Guid>.Failure("Failed to create recipe");

        return Result<Guid>.Success(recipe.Id);
      }
    }
  }
}
