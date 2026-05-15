namespace Application.DTOs.Recipes
{
  public class RecipeUpsertIngredientDto
  {
    public int SortOrder { get; set; }
    public string Name { get; set; }
    public decimal? Quantity { get; set; }
    public string Unit { get; set; }
    public string Note { get; set; }

    public decimal? Calories { get; set; }
    public decimal? ProteinGrams { get; set; }
    public decimal? CarbsGrams { get; set; }
    public decimal? FatGrams { get; set; }

    public bool HasAnyMacroProvided() =>
      Calories.HasValue
      || ProteinGrams.HasValue
      || CarbsGrams.HasValue
      || FatGrams.HasValue;
  }
}
