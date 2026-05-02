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
  public class Edit
  {
    public class Command : IRequest<Result<Unit>>
    {
      public Guid Id { get; set; }
      public RecipeUpsertDto Recipe { get; set; }
    }

    public class CommandValidator : AbstractValidator<Command>
    {
      public CommandValidator()
      {
        RuleFor(x => x.Recipe).SetValidator(new RecipeUpsertValidator());
      }
    }

    public class Handler : IRequestHandler<Command, Result<Unit>>
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

      public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
      {
        var ownerId = _userAccessor.GetUserId();
        var recipe = await _recipes.GetByIdForOwnerAsync(request.Id, ownerId, cancellationToken);
        if (recipe == null) return null;

        recipe.Title = request.Recipe.Title.Trim();
        recipe.Description = request.Recipe.Description?.Trim();
        recipe.SourceUrl = string.IsNullOrWhiteSpace(request.Recipe.SourceUrl) ? null : request.Recipe.SourceUrl.Trim();
        recipe.PrepMinutes = request.Recipe.PrepMinutes;
        recipe.CookMinutes = request.Recipe.CookMinutes;
        recipe.TotalMinutes = request.Recipe.TotalMinutes;
        recipe.Servings = request.Recipe.Servings;
        recipe.UpdatedUtc = DateTime.UtcNow;

        recipe.Ingredients.Clear();
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

        recipe.Steps.Clear();
        foreach (var step in request.Recipe.Steps.OrderBy(s => s.SortOrder))
        {
          recipe.Steps.Add(new RecipeStep
          {
            Id = Guid.NewGuid(),
            SortOrder = step.SortOrder,
            Instruction = step.Instruction.Trim()
          });
        }

        recipe.Tags.Clear();
        foreach (var tag in request.Recipe.Tags.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
          recipe.Tags.Add(new RecipeTag
          {
            Id = Guid.NewGuid(),
            TagName = tag.Trim()
          });
        }

        var ok = await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;
        if (!ok) return Result<Unit>.Failure("Failed to update recipe");

        return Result<Unit>.Success(Unit.Value);
      }
    }
  }
}
