using Application.DTOs.Nutrition;
using Domain;

namespace Application.Commands.Recipes
{
  public static class RecipeNutritionUpdater
  {
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
