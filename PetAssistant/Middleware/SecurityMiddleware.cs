using System.Text;

namespace PetAssistant.Middleware;

public class SecurityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityMiddleware> _logger;

    public SecurityMiddleware(RequestDelegate next, ILogger<SecurityMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers
        AddSecurityHeaders(context);

        // Check request size
        if (context.Request.ContentLength > 10_485_760) // 10MB limit
        {
            context.Response.StatusCode = 413;
            await context.Response.WriteAsync("Request too large");
            return;
        }

        // Check for suspicious patterns in URL
        if (ContainsSuspiciousPatterns(context.Request.Path))
        {
            _logger.LogWarning("Suspicious request path detected: {Path} from {IP}", 
                context.Request.Path, context.Connection.RemoteIpAddress);
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Bad request");
            return;
        }

        await _next(context);
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;
        
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["X-XSS-Protection"] = "1; mode=block";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:";
        
        // Remove server header
        headers.Remove("Server");
    }

    private bool ContainsSuspiciousPatterns(string path)
    {
        var suspiciousPatterns = new[]
        {
            "../", "..\\", "%2e%2e", "%2f", "%5c",
            "<script", "</script", "javascript:",
            "cmd.exe", "powershell", "/etc/passwd"
        };

        return suspiciousPatterns.Any(pattern => 
            path.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }
}

public static class SecurityMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityMiddleware>();
    }
}