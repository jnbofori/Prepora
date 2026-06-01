using Application.DTOs.Recipes;

namespace Application.DTOs.Nutrition
{
  public class CalculateNutritionRequest
  {
    public ICollection<RecipeUpsertIngredientDto> Ingredients { get; set; } = new List<RecipeUpsertIngredientDto>();
    public decimal Servings { get; set; } = 1;
  }
}
