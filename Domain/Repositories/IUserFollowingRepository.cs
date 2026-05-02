using Domain;

namespace Domain.Repositories
{
  public interface IUserFollowingRepository
  {
    Task<UserFollowing?> FindAsync(string observerId, string targetId, CancellationToken cancellationToken = default);
    void Add(UserFollowing following);
    void Remove(UserFollowing following);
  }
}
