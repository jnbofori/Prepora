using System.Text;
using API.Services;
using Domain;
using Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Persistence;

namespace API.Extensions
{
  public static class IdentityServiceExtensions
  {
    public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration config)
    {
      services.AddIdentityCore<AppUser>(opt =>
      {
        opt.Password.RequireNonAlphanumeric = false;
        opt.User.RequireUniqueEmail = true;
      })
      .AddEntityFrameworkStores<DataContext>(); // note: this allows us to query our entity framework store (ie. the db)

      var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["TokenKey"]));

      services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
          .AddJwtBearer(opt =>
          {
            opt.TokenValidationParameters = new TokenValidationParameters
            {
              // note: telling it what to validate
              ValidateIssuerSigningKey = true,
              IssuerSigningKey = key,
              ValidateIssuer = false,
              ValidateAudience = false
            };
            // note: below is to implement authentication for signalR hub
            opt.Events = new JwtBearerEvents
            {
              OnMessageReceived = context => 
              {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && (path.StartsWithSegments("/chat")))
                {
                  context.Token = accessToken;
                }
                return Task.CompletedTask;
              }
            };
          });

      services.AddAuthorization(opt =>
      {
        opt.AddPolicy("IsActivityHost", policy =>
        {
          policy.Requirements.Add(new IsHostRequirement());
        });
        opt.AddPolicy("IsRecipeOwner", policy =>
        {
          policy.Requirements.Add(new IsRecipeOwnerRequirement());
        });
      });
      services.AddTransient<IAuthorizationHandler, IsHostRequirementHandler>();
      services.AddTransient<IAuthorizationHandler, IsRecipeOwnerHandler>();
      
      // note: this means the token service is scoped to the http request itself
      services.AddScoped<TokenService>();

      return services;
    }
  }
}