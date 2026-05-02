namespace Domain
{
  public class Recipe
  {
    public Guid Id { get; set; }

    public string OwnerId { get; set; }
    public AppUser Owner { get; set; }

    public string Title { get; set; }
    public string Description { get; set; }
    public string SourceUrl { get; set; }

    public int? PrepMinutes { get; set; }
    public int? CookMinutes { get; set; }
    public int? TotalMinutes { get; set; }

    public decimal Servings { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }

    public ICollection<RecipeIngredient> Ingredients { get; set; } = new List<RecipeIngredient>();
    public ICollection<RecipeStep> Steps { get; set; } = new List<RecipeStep>();
    public ICollection<RecipeTag> Tags { get; set; } = new List<RecipeTag>();
    public ICollection<RecipePhoto> Photos { get; set; } = new List<RecipePhoto>();
  }
}
