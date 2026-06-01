namespace Application.DTOs.Recipes
{
  public class ParseIngredientsResultDto
  {
    public ICollection<ParsedIngredientLineDto> Ingredients { get; set; } = new List<ParsedIngredientLineDto>();
    public ICollection<string> Warnings { get; set; } = new List<string>();
  }
}
