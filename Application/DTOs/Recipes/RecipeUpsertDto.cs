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

    public ICollection<RecipeUpsertIngredientDto> Ingredients { get; set; } = new List<RecipeUpsertIngredientDto>();
    public ICollection<RecipeUpsertStepDto> Steps { get; set; } = new List<RecipeUpsertStepDto>();
    public ICollection<string> Tags { get; set; } = new List<string>();
  }
}
