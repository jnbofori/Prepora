using Domain;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories
{
  public class UserRepository : IUserRepository
  {
    private readonly DataContext _context;

    public UserRepository(DataContext context)
    {
      _context = context;
    }

    public Task<AppUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
      _context.Users.FirstOrDefaultAsync(x => x.UserName == username, cancellationToken);

    public Task<AppUser?> GetByUsernameWithPhotosAsync(string username, CancellationToken cancellationToken = default) =>
      _context.Users.Include(p => p.Photos).FirstOrDefaultAsync(x => x.UserName == username, cancellationToken);
  }
}
