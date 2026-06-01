using Application.DTOs.Recipes;

namespace Application.Interfaces
{
  public interface IIngredientParserService
  {
    Task<ParseIngredientsResultDto> ParseAsync(IReadOnlyList<string> lines, CancellationToken cancellationToken = default);
  }
}
