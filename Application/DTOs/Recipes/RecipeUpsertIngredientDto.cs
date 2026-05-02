namespace Application.DTOs.Recipes
{
  public class RecipeUpsertIngredientDto
  {
    public int SortOrder { get; set; }
    public string Name { get; set; }
    public decimal? Quantity { get; set; }
    public string Unit { get; set; }
    public string Note { get; set; }
  }
}
