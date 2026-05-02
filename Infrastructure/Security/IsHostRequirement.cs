using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Infrastructure.Security
{
  public class IsHostRequirement : IAuthorizationRequirement
  {
  }

  public class IsHostRequirementHandler : AuthorizationHandler<IsHostRequirement>
  {
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly DataContext _dbContext;
    public IsHostRequirementHandler(DataContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
      _dbContext = dbContext;
      _httpContextAccessor = httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, IsHostRequirement requirement)
    {
      var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

      if (userId == null) return Task.CompletedTask;

      var activityId = Guid.Parse(_httpContextAccessor.HttpContext?.Request.RouteValues
        .SingleOrDefault(x => x.Key == "id").Value?.ToString());

      // note: we used 'Result' here because we can't use await (its an override method)
      // note: when getting attendee object here this tracks the entity we're getting and it 
      // stays in memory, even though handler will be disposed of. This was causing an error 
      // when updating activities
      var attendee = _dbContext.ActivityAttendees
        .AsNoTracking()
        .SingleOrDefaultAsync(x => x.AppUserId == userId && x.ActivityId == activityId).Result;

      if (attendee == null) return Task.CompletedTask;

      if (attendee.IsHost) context.Succeed(requirement);
      
      return Task.CompletedTask;
    }
  }
}