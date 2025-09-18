using JWT.Algorithms;
using JWT.Builder;

namespace PetAssistant.Services.Auth;

public interface IJwtTokenUsageService
{
    string GenerateToken(string userId);
    string GetJwtId(string token);
    TimeSpan TokenLifetime { get; }
}

public class JwtTokenService : IJwtTokenUsageService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtTokenService> _logger;
    private readonly int _expirationMinutes;

    public TimeSpan TokenLifetime => TimeSpan.FromMinutes(_expirationMinutes);

    public JwtTokenService(IConfiguration configuration, ILogger<JwtTokenService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _expirationMinutes = _configuration.GetValue<int>("JwtSettings:ExpirationMinutes");
    }

    public string GenerateToken(string userId)
    {
        var jti = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow;
        var expiry = now.AddMinutes(_expirationMinutes);

        _logger.LogInformation("Generating token for user {UserId} with JTI {JTI}. Now: {Now}, Expiry: {Expiry}, Minutes: {Minutes}",
            userId, jti, now, expiry, _expirationMinutes);

        var token = JwtBuilder.Create()
            .WithAlgorithm(new HMACSHA256Algorithm())
            .WithSecret(_configuration["JwtSettings:SecretKey"])
            .AddClaim("nameid", userId)
            .AddClaim("jti", jti)
            .AddClaim("iat", now.ToUnixTimeSeconds())
            .AddClaim("exp", expiry.ToUnixTimeSeconds())
            .AddClaim("iss", _configuration["JwtSettings:Issuer"])
            .AddClaim("aud", _configuration["JwtSettings:Audience"])
            .Encode();

        return token;
    }


    public string GetJwtId(string token)
    {
        var payload = JwtBuilder.Create()
            .DoNotVerifySignature()
            .Decode<IDictionary<string, object>>(token);

        return payload.TryGetValue("jti", out var jtiValue) ? jtiValue?.ToString() ?? string.Empty : string.Empty;
    }
}