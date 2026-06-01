namespace Application.DTOs.Nutrition
{
  public class RecipeNutritionResult
  {
    public decimal? Calories { get; set; }
    public decimal? ProteinGrams { get; set; }
    public decimal? CarbsGrams { get; set; }
    public decimal? FatGrams { get; set; }

    public decimal? CaloriesPerServing { get; set; }
    public decimal? ProteinGramsPerServing { get; set; }
    public decimal? CarbsGramsPerServing { get; set; }
    public decimal? FatGramsPerServing { get; set; }
    public DateTime? CalculatedUtc { get; set; }

    public ICollection<string> Warnings { get; set; } = new List<string>();

    public ICollection<IngredientNutritionResult> Ingredients { get; set; } = new List<IngredientNutritionResult>();
  }
}
