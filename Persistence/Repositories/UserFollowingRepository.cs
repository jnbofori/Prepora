using Domain;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories
{
  public class UserFollowingRepository : IUserFollowingRepository
  {
    private readonly DataContext _context;

    public UserFollowingRepository(DataContext context)
    {
      _context = context;
    }

    public Task<UserFollowing?> FindAsync(string observerId, string targetId, CancellationToken cancellationToken = default) =>
      _context.UserFollowings.FindAsync(new object[] { observerId, targetId }, cancellationToken).AsTask();

    public void Add(UserFollowing following) => _context.UserFollowings.Add(following);

    public void Remove(UserFollowing following) => _context.UserFollowings.Remove(following);
  }
}
