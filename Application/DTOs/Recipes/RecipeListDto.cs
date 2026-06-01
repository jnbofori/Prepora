namespace Application.DTOs.Recipes
{
  public class RecipeListDto
  {
    public Guid Id { get; set; }
    public string Title { get; set; }
    public decimal Servings { get; set; }
    public int? PrepMinutes { get; set; }
    public int? CookMinutes { get; set; }
    public int? TotalMinutes { get; set; }
    public decimal? CaloriesPerServing { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public ICollection<string> Tags { get; set; } = new List<string>();
    public string CoverImageUrl { get; set; }
  }
}
