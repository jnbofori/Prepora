namespace Application.DTOs.Recipes
{
  public class RecipeStepDto
  {
    public Guid Id { get; set; }
    public int SortOrder { get; set; }
    public string Instruction { get; set; }
  }
}
