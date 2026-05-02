using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Infrastructure.Security
{
  public class IsRecipeOwnerRequirement : IAuthorizationRequirement
  {
  }

  public class IsRecipeOwnerHandler : AuthorizationHandler<IsRecipeOwnerRequirement>
  {
    private readonly DataContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public IsRecipeOwnerHandler(DataContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
      _dbContext = dbContext;
      _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(
      AuthorizationHandlerContext context,
      IsRecipeOwnerRequirement requirement)
    {
      var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
      if (userId == null) return;

      var idValue = _httpContextAccessor.HttpContext?.Request.RouteValues["id"]?.ToString();
      if (!Guid.TryParse(idValue, out var recipeId)) return;

      var owns = await _dbContext.Recipes
        .AsNoTracking()
        .AnyAsync(r => r.Id == recipeId && r.OwnerId == userId);

      if (owns) context.Succeed(requirement);
    }
  }
}
