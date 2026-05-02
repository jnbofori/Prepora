using Domain.Repositories;

namespace Persistence.Repositories
{
  public class UnitOfWork : IUnitOfWork
  {
    private readonly DataContext _context;

    public UnitOfWork(DataContext context)
    {
      _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
      _context.SaveChangesAsync(cancellationToken);
  }
}
