namespace Domain
{
  public class RecipePhoto
  {
    public string Id { get; set; }
    public string Url { get; set; }
    public bool IsCover { get; set; }
    public int SortOrder { get; set; }

    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; }
  }
}
