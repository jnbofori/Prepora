namespace Application.DTOs.Recipes
{
  public class RecipeIngredientDto
  {
    public Guid Id { get; set; }
    public int SortOrder { get; set; }
    public string Name { get; set; }
    public decimal? Quantity { get; set; }
    public string Unit { get; set; }
    public string Note { get; set; }
  }
}
