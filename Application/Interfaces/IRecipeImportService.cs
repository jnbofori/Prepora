using Application.DTOs.Recipes;

namespace Application.Interfaces
{
  public interface IRecipeImportService
  {
    Task<RecipeImportPreviewDto> ImportPreviewAsync(string url, CancellationToken cancellationToken = default);
  }
}
