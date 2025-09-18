using PetAssistant.Services.Redis; 

namespace PetAssistant.Services.Auth;

public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token, string Username, DateTime ExpiresAt);
public record UserInfo(string Id, string Username, string PasswordHash);

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task LogoutAsync(string jwtId);
}

public class AuthenticationService : IAuthService
{
    private readonly IJwtTokenUsageService _jwtTokenService;
    private readonly IJwtTokenCacheService _tokenStorageService;
    private readonly ILogger<AuthenticationService> _logger;

    // Hardcoded test user - In production, this would come from a database
    private static readonly UserInfo _testUser = new(
        Id: "test-user-001",
        Username: "testuser",
        PasswordHash: BCrypt.Net.BCrypt.HashPassword("testuser123") // Pre-hashed password for testuser123
    );

    public AuthenticationService(
        IJwtTokenUsageService jwtTokenCreation,
        IJwtTokenCacheService tokenStorageService,
        ILogger<AuthenticationService> logger)
    {
        _jwtTokenService = jwtTokenCreation;
        _tokenStorageService = tokenStorageService;
        _logger = logger;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        try
        {
            // Validate credentials against hardcoded user
            if (!ValidateCredentials(request.Username, request.Password))
            {
                _logger.LogWarning("Failed login attempt for username: {Username}", request.Username);
                return null;
            }

            // Generate JWT token
            var token = _jwtTokenService.GenerateToken(_testUser.Id);
            var jwtId = _jwtTokenService.GetJwtId(token);
            
            // Store token in Redis
            await _tokenStorageService.CacheTokenAsync(jwtId, _testUser.Id, _jwtTokenService.TokenLifetime);

            var expiresAt = DateTime.UtcNow.Add(_jwtTokenService.TokenLifetime);
            
            _logger.LogInformation("Successful login for user: {Username}", request.Username);
            
            return new LoginResponse(token, _testUser.Username, expiresAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login process for username: {Username}", request.Username);
            return null;
        }
    }

    public async Task LogoutAsync(string jwtId)
    {
        try
        {
            await _tokenStorageService.RevokeTokenAsync(jwtId);
            _logger.LogInformation("Token revoked: {JwtId}", jwtId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout process for token: {JwtId}", jwtId);
        }
    }

    private bool ValidateCredentials(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return false;

        // Check against hardcoded test user
        if (!string.Equals(username, _testUser.Username, StringComparison.OrdinalIgnoreCase))
            return false;

        return BCrypt.Net.BCrypt.Verify(password, _testUser.PasswordHash);
    }

}