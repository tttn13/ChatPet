using Microsoft.AspNetCore.Authorization;
using PetAssistant.Services.Auth;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using PetAssistant.Services;

namespace PetAssistant.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/login", async (
            LoginRequest request,
            IAuthService authService,
            HttpContext context,
            ILogger<Program> logger) =>
        {
            try
            {
                var result = await authService.LoginAsync(request);

                if (result == null)
                {
                    return Results.Unauthorized();
                }

                context.Response.Cookies.Append("X-Access-Token", result.Token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None, // Changed from Strict to None for cross-origin
                    Expires = result.ExpiresAt
                });
                var hasCookie = context.Request.Cookies.TryGetValue("X-Access-Token", out var cookieToken);

                return Results.Ok(new
                {
                    usesCookie = hasCookie,
                    cookieLength = hasCookie ? cookieToken.Length : 0,
                    username = result.Username,
                    expiresAt = result.ExpiresAt,
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during login request");
                return Results.Problem("An error occurred during login", statusCode: 500);
            }
        })
        .WithName("Login")
        .WithSummary("Authenticate user and generate JWT token")
        .WithDescription("Login with username and password to receive a JWT token for API authentication")
        .Produces<object>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/logout", [Authorize] async (
            ClaimsPrincipal user,
            IAuthService authService,
            IJwtTokenUsageService jwtTokenService,
            HttpContext context,
            ILogger<Program> logger) =>
        {
            try
            {
                string token = await context.GetTokenAsync("access_token");

                if (string.IsNullOrWhiteSpace(token))
                {
                    return Results.BadRequest(new { message = "Token not found" });
                }

                var jwtId = jwtTokenService.GetJwtId(token);

                await authService.LogoutAsync(jwtId);

                context.Response.Cookies.Delete("X-Access-Token");

                return Results.Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during logout request");
                return Results.Problem("An error occurred during logout", statusCode: 500);
            }
        })
        .WithName("Logout")
        .WithSummary("Logout and revoke JWT token")
        .WithDescription("Revoke the current JWT token to invalidate the session")
        .Produces<object>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status500InternalServerError);

        group.MapGet("/me", [Authorize] (ClaimsPrincipal user, HttpContext context) =>
        {
            var username = user.FindFirst(ClaimTypes.Name)?.Value;
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var hasCookie = context.Request.Cookies.TryGetValue("X-Access-Token", out var cookieToken);

            return Results.Ok(new
            {
                claims = user.Claims.Select(c => new { type = c.Type, value = c.Value }).ToList(),
                username,
                userId,
                isAuthenticated = true
            });
        })
        .WithName("GetCurrentUser")
        .WithSummary("Get current user information")
        .WithDescription("Retrieve information about the currently authenticated user")
        .Produces<object>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/validate", [Authorize] (ClaimsPrincipal user) =>
        {
            return Results.Ok(new
            {
                valid = true,
                username = user.FindFirst(ClaimTypes.Name)?.Value,
                expiresAt = user.FindFirst("exp")?.Value
            });
        })
        .WithName("ValidateToken")
        .WithSummary("Validate JWT token")
        .WithDescription("Check if the current JWT token is valid and not expired")
        .Produces<object>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);

        // Discord OAuth login
        group.MapGet("/discord", async (HttpContext context, IConfiguration config) =>
        {
            var redirectUri = config["Discord:RedirectUri"];
            var clientId = config["Discord:ClientId"];
            var scopes = "identify email";

            var authUrl = $"https://discord.com/api/oauth2/authorize?response_type=code&client_id={clientId}&scope={scopes}&redirect_uri={Uri.EscapeDataString(redirectUri)}";

            return Results.Redirect(authUrl);
        })
        .WithName("DiscordLogin")
        .WithSummary("Initiate Discord OAuth login")
        .WithDescription("Redirects to Discord OAuth authorization page");


        group.MapGet("/discord/callback", async (
            string code,
            IConfiguration config,
            IAuthService authService,
            IDiscordApi discordApi,
            HttpContext context,
            ILogger<Program> logger) =>
        {
            try
            {
                var discordToken = await discordApi.ExchangeCodeForToken(code);
                if (string.IsNullOrWhiteSpace(discordToken))
                {
                    logger.LogError("Failed to exchange Discord code for token");
                    return Results.Problem("Failed to authenticate with Discord", statusCode: 400);
                }

                var discordUser = await discordApi.GetUser(discordToken);
                if (discordUser == null)
                {
                    logger.LogError("Failed to retrieve Discord user information");
                    return Results.Problem("Failed to retrieve Discord user information", statusCode: 400);
                }

                var jwtResult = await authService.LoginWithDiscordAsync(discordUser);

                context.Response.Cookies.Append("X-Access-Token", jwtResult.Token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = jwtResult.ExpiresAt
                });

                var frontendUrl = config["Discord:FrontendRedirectUrl"] ?? "http://localhost:3000";
                // return Results.Redirect($"{frontendUrl}?login=success&provider=discord");
                return Results.Redirect($"{frontendUrl}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during Discord OAuth callback");
                return Results.Problem("An error occurred during Discord authentication", statusCode: 500);
            }
        })
        .WithName("DiscordCallback")
        .WithSummary("Handle Discord OAuth callback")
        .WithDescription("Processes the authorization code from Discord and generates JWT token");

        return group;
    }

  
}
