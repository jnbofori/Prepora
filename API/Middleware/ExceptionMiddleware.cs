using System.Net;
using System.Text.Json;
using Application.Core;

namespace API.Middleware
{
  public class ExceptionMiddleware
  {
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
    {
      _env = env;
      _next = next;
      _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context) 
    {
      try
      {
        // note: this will simply pass this on to the next middleware
        // this middleware is only meant to catch exceptions
        await _next(context);
      }
      catch (Exception e)
      {
        _logger.LogError(e, e.Message);
        // note: we need to specify this cos we're not in an API controller context
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = _env.IsDevelopment()
          ? new AppException(context.Response.StatusCode, e.Message, e.StackTrace?.ToString())
          : new AppException(context.Response.StatusCode, "Internal Server Error");

        // note: we need to specify this cos we're not in an API controller context
        var options = new JsonSerializerOptions{PropertyNamingPolicy = JsonNamingPolicy.CamelCase};

        var json = JsonSerializer.Serialize(response, options);

        await context.Response.WriteAsync(json);
      }
    }
  }
}