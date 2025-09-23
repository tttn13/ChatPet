using PetAssistant.Models;
using System.Text.Json;

namespace PetAssistant.Services;

public interface IDiscordApi
{
    Task<DiscordUser?> GetUser(string accessToken);
    Task<string?> ExchangeCodeForToken(string code);
}

public class DiscordApi : BaseService, IDiscordApi
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public DiscordApi(HttpClient httpClient, IConfiguration configuration, ILogger<DiscordApi> logger)
        : base(logger)
    {
        _httpClient = httpClient;
        _config = configuration;
        _httpClient.BaseAddress = new Uri("https://discord.com/api/");
    }

    public async Task<string?> ExchangeCodeForToken(string code)
    {
        var clientId = _config["Discord:ClientId"];
        var clientSecret = _config["Discord:ClientSecret"];
        var redirectUri = _config["Discord:RedirectUri"];

        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", clientId!),
            new KeyValuePair<string, string>("client_secret", clientSecret!),
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", redirectUri!)
        });
        var response = await _httpClient.PostAsync("oauth2/token", tokenRequest);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            LogInfo("Discord token exchange failed - Status: {StatusCode}, Error: {Error}, RedirectUri: {RedirectUri}",
                response.StatusCode, errorBody, redirectUri);
            return null;
        }

        var tresponse = await response.Content.ReadFromJsonAsync<DiscordAuthResponse>();
        
        return tresponse.AccessToken;
    }
    public async Task<DiscordUser?> GetUser(string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

        var response = await _httpClient.GetAsync("users/@me");
        if (!response.IsSuccessStatusCode) return null;

        var userResponse = await response.Content.ReadFromJsonAsync<DiscordUser>();
        Console.WriteLine($"user id is {userResponse.Id}");

        return userResponse;
    }
}