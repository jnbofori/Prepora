using Domain;

namespace Domain.Repositories
{
  public interface IUserRepository
  {
    Task<AppUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<AppUser?> GetByUsernameWithPhotosAsync(string username, CancellationToken cancellationToken = default);
  }
}
