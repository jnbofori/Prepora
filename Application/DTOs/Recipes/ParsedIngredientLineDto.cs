namespace Application.DTOs.Recipes
{
  public class ParsedIngredientLineDto
  {
    public int SortOrder { get; set; }
    public string Name { get; set; }
    public decimal? Quantity { get; set; }
    public string Unit { get; set; }
    public string Size { get; set; }
    public string Note { get; set; }
    public bool Branded { get; set; }
  }
}
