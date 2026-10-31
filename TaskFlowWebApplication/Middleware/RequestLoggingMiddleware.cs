using Microsoft.AspNetCore.Routing.Patterns;
using System.Diagnostics;
using System.Security.Claims;

namespace TaskFlowWebApplication.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;
        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }
        public async Task InvokeAsync (HttpContext context)
        {
            var stopWatch = Stopwatch.StartNew();
            int statusCode = StatusCodes.Status200OK;
            try
            {
                await _next(context);
                statusCode = context.Response.StatusCode;
            }
            catch(Exception)
            {
                statusCode = StatusCodes.Status500InternalServerError;
                throw;
            }
            finally
            {
                stopWatch.Stop();
                string method = context.Request.Method;
                string route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "unknow route";
                string userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
                string traceId = context.TraceIdentifier;
                double elapsedMs = stopWatch.Elapsed.TotalMilliseconds;
                if (statusCode >= 400)
                {
                    _logger.LogWarning(
                        "HTTP {Method} {Route} -> {StatusCode} | " +
                        "UserId: {UserId} | Duration: {ElapsedMs} ms | " + "TraceId: {TraceId}",
                        method, route, statusCode, userId, elapsedMs, traceId);
                }
                else
                {
                    _logger.LogInformation(
                        "HTTP {Method} {Route} -> {StatusCode} | " +
                        "UserId: {UserId} | Duration: {ElapsedMs} ms | " + "TraceId: {TraceId}",
                        method, route, statusCode,userId, elapsedMs, traceId);
                }
            }
        }
    }
}
