using Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Repositories;

namespace Persistence
{
  public static class PersistenceServiceExtensions
  {
    public static IServiceCollection AddPersistenceRepositories(this IServiceCollection services)
    {
      services.AddScoped<IUnitOfWork, UnitOfWork>();
      services.AddScoped<IActivityRepository, ActivityRepository>();
      services.AddScoped<IUserRepository, UserRepository>();
      services.AddScoped<IUserFollowingRepository, UserFollowingRepository>();
      services.AddScoped<IRecipeRepository, RecipeRepository>();
      return services;
    }
  }
}
