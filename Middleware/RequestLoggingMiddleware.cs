using System.Diagnostics;

namespace AeroCloud.PPS.Middleware;

/// <summary>
/// ASP.NET Core middleware that sits in the pipeline and times every inbound request,
/// logging method, path, response status and elapsed milliseconds as structured fields.
///
/// Registered in Program.cs via app.UseRequestLogging() before routing so it wraps
/// the entire controller execution.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            await _next(context); // hand off to the rest of the pipeline
        }
        finally
        {
            sw.Stop();

            // Structured log — each {} becomes a named property in a log aggregator
            // (Azure Monitor, Seq, etc.) rather than a plain string
            _logger.LogInformation(
                "{Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds);
        }
    }
}

/// <summary>
/// Extension method so Program.cs reads as app.UseRequestLogging()
/// rather than app.UseMiddleware&lt;RequestLoggingMiddleware&gt;().
/// </summary>
public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
        => app.UseMiddleware<RequestLoggingMiddleware>();
}
