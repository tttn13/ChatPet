using PetAssistant.Models;
using System.IdentityModel.Tokens.Jwt;
namespace PetAssistant.Services;

public interface IGoogleApi
{
    Task<AuthUser?> GetUser(string idToken);
    Task<GoogleAuthResponse> ExchangeCodeForToken(string code);
}

public class GoogleApi : BaseService, IGoogleApi
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    public GoogleApi(IConfiguration config, HttpClient httpClient, ILogger<GoogleApi> logger) : base(logger)
    {
        _httpClient = httpClient;
        _config = config;
        _httpClient.BaseAddress = new Uri("https://oauth2.googleapis.com/");
    }
   
    public async Task<GoogleAuthResponse> ExchangeCodeForToken(string code)
    {
        var clientId = _config["Google:ClientId"];
        var clientSecret = _config["Google:ClientSecret"];
        var redirectUri = _config["Google:RedirectUri"];

        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", clientId!),
            new KeyValuePair<string, string>("client_secret", clientSecret!),
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", redirectUri!)
        });
        var response = await _httpClient.PostAsync("token", tokenRequest);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            LogInfo("Google token exchange failed - Status: {StatusCode}, Error: {Error}, RedirectUri: {RedirectUri}",
                response.StatusCode, errorBody, redirectUri);
            return null;
        }

        var coderesponse = await response.Content.ReadFromJsonAsync<GoogleAuthResponse>();
        return coderesponse;
    }

    public async Task<AuthUser?> GetUser(string idToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(idToken);
        var email = jsonToken.Claims.First(c => c.Type  == "email")?.Value;
        var name = jsonToken.Claims.First(c => c.Type == "name")?.Value;
        var sub = jsonToken.Claims.First(c => c.Type == "sub")?.Value;
        var ava = jsonToken.Claims.First(c => c.Type == "picture")?.Value;

        return new AuthUser
        {
            Id = sub,
            Username = email,
            Email = email
        };
    }
}
