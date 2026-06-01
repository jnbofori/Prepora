namespace Application.DTOs.Recipes
{
  public class ParseIngredientsRequest
  {
    /// <summary>Individual ingredient lines to parse.</summary>
    public ICollection<string> Lines { get; set; } = new List<string>();

    /// <summary>Alternative to Lines: newline-separated ingredient text.</summary>
    public string Text { get; set; }
  }
}
