using Microsoft.AspNetCore.Diagnostics;

namespace TaskFlowWebApplication.Middleware
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext context,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(exception,"An unexpected error occurred.");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Internal Server Error",
                    traceId = context.TraceIdentifier
                },cancellationToken);
            return true;
        }
    }
}