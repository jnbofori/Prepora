using Application.Core;
using Application.Interfaces;
using Domain;
using Domain.Repositories;
using MediatR;

namespace Application.Commands.Recipes
{
  public class Duplicate
  {
    public class Command : IRequest<Result<Guid>>
    {
      public Guid Id { get; set; }
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
        var source = await _recipes.GetByIdForOwnerAsync(request.Id, ownerId, cancellationToken);
        if (source == null) return null;

        var now = DateTime.UtcNow;
        var copy = new Recipe
        {
          Id = Guid.NewGuid(),
          OwnerId = ownerId,
          Title = $"{source.Title} (Copy)",
          Description = source.Description,
          SourceUrl = source.SourceUrl,
          PrepMinutes = source.PrepMinutes,
          CookMinutes = source.CookMinutes,
          TotalMinutes = source.TotalMinutes,
          Servings = source.Servings,
          CreatedUtc = now,
          UpdatedUtc = now
        };

        foreach (var ing in source.Ingredients.OrderBy(i => i.SortOrder))
        {
          copy.Ingredients.Add(new RecipeIngredient
          {
            Id = Guid.NewGuid(),
            SortOrder = ing.SortOrder,
            Name = ing.Name,
            Quantity = ing.Quantity,
            Unit = ing.Unit,
            Note = ing.Note
          });
        }

        foreach (var step in source.Steps.OrderBy(s => s.SortOrder))
        {
          copy.Steps.Add(new RecipeStep
          {
            Id = Guid.NewGuid(),
            SortOrder = step.SortOrder,
            Instruction = step.Instruction
          });
        }

        foreach (var tag in source.Tags)
        {
          copy.Tags.Add(new RecipeTag
          {
            Id = Guid.NewGuid(),
            TagName = tag.TagName
          });
        }

        _recipes.Add(copy);

        var ok = await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;
        if (!ok) return Result<Guid>.Failure("Failed to duplicate recipe");

        return Result<Guid>.Success(copy.Id);
      }
    }
  }
}
