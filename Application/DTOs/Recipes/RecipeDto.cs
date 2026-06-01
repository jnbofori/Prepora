namespace Application.DTOs.Recipes
{
  public class RecipeDto
  {
    public Guid Id { get; set; }

    public string Title { get; set; }
    public string Description { get; set; }
    public string SourceUrl { get; set; }

    public int? PrepMinutes { get; set; }
    public int? CookMinutes { get; set; }
    public int? TotalMinutes { get; set; }

    public decimal Servings { get; set; }

    public decimal? Calories { get; set; }
    public decimal? ProteinGrams { get; set; }
    public decimal? CarbsGrams { get; set; }
    public decimal? FatGrams { get; set; }

    public decimal? CaloriesPerServing { get; set; }
    public decimal? ProteinGramsPerServing { get; set; }
    public decimal? CarbsGramsPerServing { get; set; }
    public decimal? FatGramsPerServing { get; set; }
    public DateTime? NutritionCalculatedUtc { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }

    public ICollection<RecipeIngredientDto> Ingredients { get; set; } = new List<RecipeIngredientDto>();
    public ICollection<RecipeStepDto> Steps { get; set; } = new List<RecipeStepDto>();
    public ICollection<string> Tags { get; set; } = new List<string>();
    public ICollection<RecipePhotoDto> Photos { get; set; } = new List<RecipePhotoDto>();
  }
}
