using System.Net;

namespace DBADashAI.Services.Authentication;

/// <summary>
/// Turns away requests whose Host header isn't this machine, in local mode.
///
/// Local mode listens on loopback with no authentication, which keeps other machines out but not a web page.  A page
/// the user opens can point a hostname it controls at 127.0.0.1 (DNS rebinding); the browser then treats requests to
/// http://evil.example:5055 as same-origin and sends them, and they arrive here with that hostname as their Host -
/// able to call /mcp for repository data, or /api/ai/ask, which spends model tokens too.
/// Accepting only loopback Host names - localhost, 127.0.0.0/8, ::1 - or none (HTTP/1.0) closes that, while every
/// legitimate local client, which connects by one of those names, is unaffected.
/// </summary>
public static class LoopbackHostGuard
{
    /// <summary>True for a Host the guard lets through.  <paramref name="host"/> is the name without its port.</summary>
    public static bool IsAllowed(string? host)
    {
        if (string.IsNullOrEmpty(host))
        {
            return true;
        }

        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // An IPv6 literal arrives in brackets ("[::1]").
        var address = host.StartsWith('[') && host.EndsWith(']') ? host[1..^1] : host;
        return IPAddress.TryParse(address, out var ip) && IPAddress.IsLoopback(ip);
    }

    /// <summary>Answers 400 to a request whose Host isn't a loopback name.</summary>
    public static IApplicationBuilder UseLoopbackHostGuard(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            if (!IsAllowed(context.Request.Host.Host))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            await next(context);
        });
}
