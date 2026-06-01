namespace Application.DTOs.Recipes
{
  public class RecipeUpsertDto
  {
    public string Title { get; set; }
    public string Description { get; set; }
    public string SourceUrl { get; set; }

    public int? PrepMinutes { get; set; }
    public int? CookMinutes { get; set; }
    public int? TotalMinutes { get; set; }

    public decimal Servings { get; set; }

    /// <summary>
    /// When any recipe-level value here is set, or any ingredient line includes per-line macros, values are
    /// persisted as manual nutrition and USDA calculation is skipped. Otherwise nutrition is computed via INutritionService.
    /// </summary>
    public decimal? Calories { get; set; }
    public decimal? ProteinGrams { get; set; }
    public decimal? CarbsGrams { get; set; }
    public decimal? FatGrams { get; set; }
    public decimal? CaloriesPerServing { get; set; }
    public decimal? ProteinGramsPerServing { get; set; }
    public decimal? CarbsGramsPerServing { get; set; }
    public decimal? FatGramsPerServing { get; set; }

    public bool HasAnyNutritionProvided() =>
      Calories.HasValue
      || ProteinGrams.HasValue
      || CarbsGrams.HasValue
      || FatGrams.HasValue
      || CaloriesPerServing.HasValue
      || ProteinGramsPerServing.HasValue
      || CarbsGramsPerServing.HasValue
      || FatGramsPerServing.HasValue
      || (Ingredients != null && Ingredients.Any(i => i.HasAnyMacroProvided()));

    public ICollection<RecipeUpsertIngredientDto> Ingredients { get; set; } = new List<RecipeUpsertIngredientDto>();
    public ICollection<RecipeUpsertStepDto> Steps { get; set; } = new List<RecipeUpsertStepDto>();
    public ICollection<string> Tags { get; set; } = new List<string>();
  }
}
