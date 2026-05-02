using Application.DTOs.Core;

namespace Application.DTOs.Recipes
{
  public class RecipeParams : PagingParams
  {
    public string Search { get; set; }
    public string Tag { get; set; }
  }
}
