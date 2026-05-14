using Application.DTOs.Nutrition;
using Domain;

namespace Application.Interfaces
{
  public interface INutritionService
  {
    Task<RecipeNutritionResult> CalculateRecipeAsync(
      IEnumerable<RecipeIngredient> ingredients,
      decimal servings,
      CancellationToken cancellationToken = default);
  }
}
