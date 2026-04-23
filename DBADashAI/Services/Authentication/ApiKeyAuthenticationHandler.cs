using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace DBADashAI.Services.Authentication;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKey";
    public string ApiKeyHeaderName { get; set; } = "X-API-Key";
}

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly ApiKeyManager _apiKeyManager;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ApiKeyManager apiKeyManager)
        : base(options, logger, encoder)
    {
        _apiKeyManager = apiKeyManager;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string[] validKeys;
        try
        {
            validKeys = await _apiKeyManager.GetAllValidKeysAsync(Context.RequestAborted);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to retrieve API keys");
            return AuthenticateResult.Fail("API key validation failed");
        }

        if (validKeys.Length == 0)
        {
            Logger.LogWarning("No API keys available. API key authentication will fail.");
            return AuthenticateResult.Fail("API keys not configured");
        }

        if (!Request.Headers.TryGetValue(Options.ApiKeyHeaderName, out var providedKey))
        {
            // Return NoResult (not Fail) so AllowAnonymous endpoints can proceed.
            // The FallbackPolicy will still reject unauthenticated requests on protected endpoints.
            return AuthenticateResult.NoResult();
        }

        var keyValue = providedKey.ToString();

        if (string.IsNullOrWhiteSpace(keyValue) || !ApiKeyComparer.TimingSafeContains(validKeys, keyValue))
        {
            Logger.LogWarning("Invalid API key attempt from {IpAddress}", Request.HttpContext.Connection.RemoteIpAddress);
            return AuthenticateResult.Fail("Invalid API key");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "ApiKeyUser"),
            new Claim("AuthenticationType", "ApiKey")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    }
