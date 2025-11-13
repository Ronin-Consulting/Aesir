using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using NLog;

namespace Aesir.Infrastructure.Middleware;

/// <summary>
/// Middleware that manages correlation IDs for request tracking across the application.
/// Reads X-Correlation-Id from request header or generates a new GUID if not present.
/// </summary>
public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private const string CorrelationIdKey = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        // Try to get correlation ID from request header, or generate a new one
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
                            ?? Guid.NewGuid().ToString();

        // Store correlation ID in HttpContext.Items for access by other components
        context.Items[CorrelationIdKey] = correlationId;

        // Add correlation ID to response header
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        // Make correlation ID available to NLog via MappedDiagnosticsLogicalContext
        using (ScopeContext.PushProperty(CorrelationIdKey, correlationId))
        {
            await next(context);
        }
    }
}

/// <summary>
/// Extension methods for registering CorrelationIdMiddleware.
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    /// <summary>
    /// Adds correlation ID middleware to the application pipeline.
    /// This should be called early in the pipeline, typically after UseRouting() but before UseEndpoints().
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
