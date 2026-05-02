namespace Domain
{
  public class RecipeStep
  {
    public Guid Id { get; set; }

    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; }

    public int SortOrder { get; set; }
    public string Instruction { get; set; }
  }
}
