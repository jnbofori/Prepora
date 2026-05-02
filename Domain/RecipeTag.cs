namespace Domain
{
  public class RecipeTag
  {
    public Guid Id { get; set; }

    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; }

    public string TagName { get; set; }
  }
}
