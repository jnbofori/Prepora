namespace Domain.Repositories
{
  public interface IRecipeRepository
  {
    Task<Recipe?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Recipe?> GetByIdForOwnerAsync(Guid id, string ownerId, CancellationToken cancellationToken = default);

    IQueryable<Recipe> QueryOwnedBy(string ownerId);

    void Add(Recipe recipe);

    void Remove(Recipe recipe);

    void AddIngredients(IEnumerable<RecipeIngredient> ingredients);

    void RemoveIngredients(IEnumerable<RecipeIngredient> ingredients);

    void AddSteps(IEnumerable<RecipeStep> steps);

    void RemoveSteps(IEnumerable<RecipeStep> steps);

    void AddTags(IEnumerable<RecipeTag> tags);

    void RemoveTags(IEnumerable<RecipeTag> tags);
  }
}
