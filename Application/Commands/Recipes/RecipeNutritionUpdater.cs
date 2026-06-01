using Application.DTOs.Nutrition;
using Application.DTOs.Recipes;
using Domain;

namespace Application.Commands.Recipes
{
  public static class RecipeNutritionUpdater
  {
    public static void ApplyManual(Recipe recipe, RecipeUpsertDto dto)
    {
      recipe.Calories = dto.Calories;
      recipe.ProteinGrams = dto.ProteinGrams;
      recipe.CarbsGrams = dto.CarbsGrams;
      recipe.FatGrams = dto.FatGrams;
      recipe.CaloriesPerServing = dto.CaloriesPerServing;
      recipe.ProteinGramsPerServing = dto.ProteinGramsPerServing;
      recipe.CarbsGramsPerServing = dto.CarbsGramsPerServing;
      recipe.FatGramsPerServing = dto.FatGramsPerServing;
      recipe.NutritionCalculatedUtc = null;
    }

    /// <summary>
    /// Copies per-line macros from the upsert DTO onto recipe ingredients, matched by <see cref="RecipeIngredient.SortOrder"/>.
    /// Lines with no matching DTO row have macros cleared.
    /// </summary>
    public static void ApplyManualIngredientLines(IEnumerable<RecipeIngredient> recipeIngredients, RecipeUpsertDto dto)
    {
      var lines = dto.Ingredients?.OrderBy(i => i.SortOrder).ToList() ?? new List<RecipeUpsertIngredientDto>();
      foreach (var ri in recipeIngredients.OrderBy(i => i.SortOrder))
      {
        var src = lines.FirstOrDefault(l => l.SortOrder == ri.SortOrder);
        if (src == null)
        {
          ri.Calories = null;
          ri.ProteinGrams = null;
          ri.CarbsGrams = null;
          ri.FatGrams = null;
          continue;
        }

        ri.Calories = src.Calories;
        ri.ProteinGrams = src.ProteinGrams;
        ri.CarbsGrams = src.CarbsGrams;
        ri.FatGrams = src.FatGrams;
      }
    }

    public static void Apply(Recipe recipe, RecipeNutritionResult nutrition, IEnumerable<RecipeIngredient> ingredientsToUpdate = null)
    {
      recipe.Calories = nutrition.Calories;
      recipe.ProteinGrams = nutrition.ProteinGrams;
      recipe.CarbsGrams = nutrition.CarbsGrams;
      recipe.FatGrams = nutrition.FatGrams;
      recipe.CaloriesPerServing = nutrition.CaloriesPerServing;
      recipe.ProteinGramsPerServing = nutrition.ProteinGramsPerServing;
      recipe.CarbsGramsPerServing = nutrition.CarbsGramsPerServing;
      recipe.FatGramsPerServing = nutrition.FatGramsPerServing;
      recipe.NutritionCalculatedUtc = nutrition.CalculatedUtc;

      var targets = (ingredientsToUpdate ?? recipe.Ingredients)
        .OrderBy(i => i.SortOrder)
        .ToList();

      var lines = nutrition.Ingredients?.OrderBy(l => l.SortOrder).ToList() ?? new List<IngredientNutritionResult>();
      foreach (var ing in targets)
      {
        var line = lines.FirstOrDefault(l => l.SortOrder == ing.SortOrder);
        if (line == null)
        {
          ing.Calories = null;
          ing.ProteinGrams = null;
          ing.CarbsGrams = null;
          ing.FatGrams = null;
          continue;
        }

        ing.Calories = line.Calories;
        ing.ProteinGrams = line.ProteinGrams;
        ing.CarbsGrams = line.CarbsGrams;
        ing.FatGrams = line.FatGrams;
      }
    }
  }
}
