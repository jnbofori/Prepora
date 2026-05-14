using Application.Core;
using Application.DTOs.Nutrition;
using Application.Interfaces;
using Domain;
using MediatR;

namespace Application.Commands.Recipes
{
  public class CalculateNutrition
  {
    public class Command : IRequest<Result<RecipeNutritionResult>>
    {
      public CalculateNutritionRequest Request { get; set; }
    }

    public class Handler : IRequestHandler<Command, Result<RecipeNutritionResult>>
    {
      private readonly INutritionService _nutritionService;

      public Handler(INutritionService nutritionService)
      {
        _nutritionService = nutritionService;
      }

      public async Task<Result<RecipeNutritionResult>> Handle(Command request, CancellationToken cancellationToken)
      {
        if (request.Request == null)
          return Result<RecipeNutritionResult>.Failure("Request body is required");

        var ingredients = request.Request.Ingredients?
          .Where(i => i != null && !string.IsNullOrWhiteSpace(i.Name))
          .OrderBy(i => i.SortOrder)
          .Select(i => new RecipeIngredient
          {
            Id = Guid.Empty,
            RecipeId = Guid.Empty,
            SortOrder = i.SortOrder,
            Name = i.Name.Trim(),
            Quantity = i.Quantity,
            Unit = string.IsNullOrWhiteSpace(i.Unit) ? null : i.Unit.Trim(),
            Note = string.IsNullOrWhiteSpace(i.Note) ? null : i.Note.Trim()
          })
          .ToList() ?? new List<RecipeIngredient>();

        if (!ingredients.Any())
          return Result<RecipeNutritionResult>.Failure("At least one ingredient with a name is required");

        var servings = request.Request.Servings > 0 ? request.Request.Servings : 1;

        var nutrition = await _nutritionService.CalculateRecipeAsync(ingredients, servings, cancellationToken);
        return Result<RecipeNutritionResult>.Success(nutrition);
      }
    }
  }
}
