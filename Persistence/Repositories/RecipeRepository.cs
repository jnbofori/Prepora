using Domain;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories
{
  public class RecipeRepository : IRecipeRepository
  {
    private readonly DataContext _context;

    public RecipeRepository(DataContext context)
    {
      _context = context;
    }

    public Task<Recipe?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
      _context.Recipes
        .Include(r => r.Ingredients)
        .Include(r => r.Steps)
        .Include(r => r.Tags)
        .Include(r => r.Photos)
        .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<Recipe?> GetByIdForOwnerAsync(Guid id, string ownerId, CancellationToken cancellationToken = default) =>
      _context.Recipes
        .Include(r => r.Ingredients)
        .Include(r => r.Steps)
        .Include(r => r.Tags)
        .Include(r => r.Photos)
        .FirstOrDefaultAsync(r => r.Id == id && r.OwnerId == ownerId, cancellationToken);

    public IQueryable<Recipe> QueryOwnedBy(string ownerId) =>
      _context.Recipes
        .AsNoTracking()
        .Where(r => r.OwnerId == ownerId);

    public void Add(Recipe recipe) => _context.Recipes.Add(recipe);

    public void Remove(Recipe recipe) => _context.Recipes.Remove(recipe);
  }
}
