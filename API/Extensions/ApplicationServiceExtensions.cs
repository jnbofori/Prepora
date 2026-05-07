using Application.Commands.Activities;
using Application.Core;
using Application.Queries.Activities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;
using FluentValidation;
using FluentValidation.AspNetCore;
using Application.Interfaces;
using Infrastructure.Security;
using Infrastructure.Photos;
using Infrastructure.Recipes;

namespace API.Extensions
{
  public static class ApplicationServiceExtensions
  {

    // note: 'this' is what we're extending. Where we are using this method (basically like binding)
    // 'config' is appsettings.json
    public static IServiceCollection AddApplicationServices
    (
      this IServiceCollection services,
      IConfiguration config
    )
    {
      // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
      services.AddEndpointsApiExplorer();
      services.AddSwaggerGen();

      services.AddDbContext<DataContext>(opt =>
      {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        string connStr;

        // Depending on if in development or production, use either FlyIO
        // connection string, or development connection string from env var.
        if (env == "Development")
        {
            // Use connection string from file.
            connStr = config.GetConnectionString("DefaultConnection");
        }
        else
        {
          // Use connection string provided at runtime by FlyIO.
          var connUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

          // Parse connection URL to connection string for Npgsql
          connUrl = connUrl.Replace("postgres://", string.Empty);
          var pgUserPass = connUrl.Split("@")[0];
          var pgHostPortDb = connUrl.Split("@")[1];
          var pgHostPort = pgHostPortDb.Split("/")[0];
          var pgDb = pgHostPortDb.Split("/")[1];
          var pgUser = pgUserPass.Split(":")[0];
          var pgPass = pgUserPass.Split(":")[1];
          var pgHost = pgHostPort.Split(":")[0];
          var pgPort = pgHostPort.Split(":")[1];
          var updatedHost = pgHost.Replace("flycast", "internal");

          connStr = $"Server={updatedHost};Port={pgPort};User Id={pgUser};Password={pgPass};Database={pgDb};";
        }

        // Whether the connection string came from the local development configuration file
        // or from the environment variable from FlyIO, use it to set up your DbContext.
        opt.UseNpgsql(connStr);
      });

      services.AddPersistenceRepositories();

      services.AddCors(opt =>
      {
        opt.AddPolicy("CorsPolicy", policy =>
            {
            policy.AllowAnyMethod().AllowAnyHeader().AllowCredentials()
              .WithOrigins("http://127.0.0.1:3000", "http://localhost:5173");
          });
      });
      // note: specified one of the handlers to let mediatr know where to look
      services.AddMediatR(typeof(List.Handler));
      services.AddAutoMapper(typeof(MappingProfiles).Assembly);
      services.AddFluentValidationAutoValidation();
      services.AddValidatorsFromAssemblyContaining<Create>();
      // note: so that we can utilize it inside our infrastructure project
      services.AddHttpContextAccessor();
      // note: makes it available to be injected into our Application handlers
      services.AddScoped<IUserAccessor, UserAccessor>();
      services.AddScoped<IPhotoAccessor, PhotoAccessor>();

      services.AddHttpClient("RecipeImport", client =>
      {
        client.Timeout = TimeSpan.FromSeconds(20);
      });
      services.AddScoped<IRecipeImportService, RecipeImportService>();

      services.Configure<CloudinarySettings>(config.GetSection("Cloudinary"));

      services.AddSignalR();

      return services;
    }
  }
}