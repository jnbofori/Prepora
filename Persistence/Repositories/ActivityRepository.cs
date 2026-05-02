using Domain;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories
{
  public class ActivityRepository : IActivityRepository
  {
    private readonly DataContext _context;

    public ActivityRepository(DataContext context)
    {
      _context = context;
    }

    public Task<Activity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
      _context.Activities.FindAsync(new object[] { id }, cancellationToken).AsTask();

    public Task<Activity?> GetByIdWithAttendeesAndUsersAsync(Guid id, CancellationToken cancellationToken = default) =>
      _context.Activities
        .Include(a => a.Attendees)
        .ThenInclude(u => u.AppUser)
        .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public void Add(Activity activity) => _context.Activities.Add(activity);

    public void Remove(Activity activity) => _context.Remove(activity);
  }
}
