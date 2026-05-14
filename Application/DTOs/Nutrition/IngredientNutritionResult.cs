namespace Application.DTOs.Nutrition
{
  public class IngredientNutritionResult
  {
    public int SortOrder { get; set; }
    public string Name { get; set; }
    public decimal? Calories { get; set; }
    public decimal? ProteinGrams { get; set; }
    public decimal? CarbsGrams { get; set; }
    public decimal? FatGrams { get; set; }
    public string Warning { get; set; }
  }
}
