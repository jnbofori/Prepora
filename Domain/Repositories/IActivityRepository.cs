using Domain;

namespace Domain.Repositories
{
  public interface IActivityRepository
  {
    Task<Activity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Activity?> GetByIdWithAttendeesAndUsersAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(Activity activity);
    void Remove(Activity activity);
  }
}
