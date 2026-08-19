namespace LzDetailingStore.Api.Middleware;

public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var correlationId = context.Items["CorrelationId"]?.ToString();
            logger.LogError(ex, "Unhandled exception. CorrelationId: {CorrelationId}", correlationId);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var message = environment.IsDevelopment() ? ex.Message : "An unexpected error occurred";

            await context.Response.WriteAsJsonAsync(new
            {
                error = "INTERNAL_ERROR",
                message,
                statusCode = 500
            });
        }
    }
}
