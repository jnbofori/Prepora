namespace Application.DTOs.Recipes
{
  public class RecipeImportPreviewDto
  {
    public bool Parsed { get; set; }
    public string Error { get; set; }
    public List<string> Warnings { get; set; } = new List<string>();
    public RecipeUpsertDto Recipe { get; set; }
  }
}
