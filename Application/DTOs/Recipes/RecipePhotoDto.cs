namespace Application.DTOs.Recipes
{
  public class RecipePhotoDto
  {
    public string Id { get; set; }
    public string Url { get; set; }
    public bool IsCover { get; set; }
    public int SortOrder { get; set; }
  }
}
