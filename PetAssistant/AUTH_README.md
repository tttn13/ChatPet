# Authentication System Setup and Usage

This document explains how to use the JWT authentication system with Redis token storage in your Pet Assistant API.

## Overview

The authentication system provides:
- JWT token-based authentication
- Redis storage for token validation and blacklisting
- Single test user for development
- Middleware for automatic token validation
- Secure token revocation on logout

## Required NuGet Packages

The following packages have been added to your project:
```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.8" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.2.0" />
<PackageReference Include="StackExchange.Redis" Version="2.8.16" />
```

## Configuration (appsettings.json)

The JWT settings have been configured in your `appsettings.json`:

```json
{
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyMustBeAtLeast32CharactersLongForHmacSha256ToWorkProperly",
    "Issuer": "PetAssistantAPI",
    "Audience": "PetAssistantClients",
    "ExpirationMinutes": 60
  },
  "Redis": {
    "ConnectionString": "localhost:6379",
    "InstanceName": "PetAssistant",
    "ConnectTimeout": 5000,
    "SyncTimeout": 5000,
    "AbortOnConnectFail": false
  }
}
```

### ⚠️ IMPORTANT: Generate Your Own JWT Secret

**For production, you MUST generate a strong JWT secret key!**

You can generate a secure key using:

1. **PowerShell** (Windows):
   ```powershell
   [System.Convert]::ToBase64String((1..64 | ForEach {Get-Random -Maximum 256}))
   ```

2. **Command Line** (Linux/Mac):
   ```bash
   openssl rand -base64 64
   ```

3. **Online Generator** (for development only):
   - Use a secure online generator like: https://generate-secret.vercel.app/64

4. **C# Code**:
   ```csharp
   var key = new byte[64];
   using (var rng = RandomNumberGenerator.Create())
   {
       rng.GetBytes(key);
   }
   var secretKey = Convert.ToBase64String(key);
   ```

Replace the `SecretKey` value in your `appsettings.json` with your generated key.

## Test User Credentials

For development and testing, a single test user is hardcoded:

- **Username**: `testuser`
- **Password**: `passwordTest@!`

## API Endpoints

### Authentication Endpoints

#### 1. Login
```
POST /api/auth/login
Content-Type: application/json

{
  "username": "testuser",
  "password": "passwordTest@!"
}
```

**Success Response** (200 OK):
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "username": "testuser",
  "expiresAt": "2024-01-01T12:00:00.000Z",
  "tokenType": "Bearer"
}
```

**Error Response** (401 Unauthorized):
```json
{
  "message": "Invalid credentials"
}
```

#### 2. Logout
```
POST /api/auth/logout
Authorization: Bearer <your-jwt-token>
```

**Success Response** (200 OK):
```json
{
  "message": "Logged out successfully"
}
```

#### 3. Get Current User
```
GET /api/auth/me
Authorization: Bearer <your-jwt-token>
```

**Success Response** (200 OK):
```json
{
  "username": "testuser",
  "userId": "test-user-001",
  "isAuthenticated": true
}
```

#### 4. Validate Token
```
GET /api/auth/validate
Authorization: Bearer <your-jwt-token>
```

**Success Response** (200 OK):
```json
{
  "valid": true,
  "username": "testuser",
  "expiresAt": "1735689600"
}
```

## Protecting Endpoints

To protect your existing endpoints with authentication, add the `[Authorize]` attribute:

### Example 1: Protecting a Single Endpoint
```csharp
using Microsoft.AspNetCore.Authorization;

group.MapGet("/protected-endpoint", [Authorize] (ClaimsPrincipal user) =>
{
    var username = user.FindFirst(ClaimTypes.Name)?.Value;
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
    return Results.Ok(new { message = $"Hello, {username}!" });
});
```

### Example 2: Protecting an Entire Endpoint Group
```csharp
// Protect all chat endpoints
api.MapGroup("/chat")
   .RequireAuthorization()  // This protects ALL endpoints in this group
   .MapChatEndpoints();
```

### Example 3: Mixed Authentication (some protected, some public)
```csharp
var chatGroup = api.MapGroup("/chat");

// Public endpoint
chatGroup.MapGet("/public", () => "This is public");

// Protected endpoint
chatGroup.MapPost("/secure", [Authorize] (ChatRequest request, ClaimsPrincipal user) =>
{
    var username = user.FindFirst(ClaimTypes.Name)?.Value;
    // Handle authenticated chat request
    return Results.Ok($"Chat from {username}");
});
```

## Testing the Authentication Flow

### 1. Start Redis
Make sure Redis is running:
```bash
# Using Docker
docker run -d -p 6379:6379 redis:alpine

# Or install locally and run
redis-server
```

### 2. Start Your Application
```bash
dotnet run
```

### 3. Test Login
```bash
curl -X POST "https://localhost:7001/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "password": "passwordTest@!"
  }'
```

### 4. Use the Token
Copy the token from the login response and use it in subsequent requests:

```bash
curl -X GET "https://localhost:7001/api/auth/me" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE"
```

### 5. Test Protected Endpoints
Try accessing a protected endpoint without a token (should get 401):
```bash
curl -X GET "https://localhost:7001/api/chat/protected"
```

Then try with a valid token (should work):
```bash
curl -X GET "https://localhost:7001/api/chat/protected" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE"
```

### 6. Test Logout
```bash
curl -X POST "https://localhost:7001/api/auth/logout" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE"
```

After logout, the token should be invalid for future requests.

## Security Features

### Token Storage in Redis
- Tokens are stored in Redis with automatic expiration
- Revoked tokens are immediately removed from Redis
- Supports bulk revocation of all user tokens

### Token Validation Middleware
- Custom middleware validates tokens against Redis on each request
- Expired or revoked tokens are automatically rejected
- Provides detailed logging for security monitoring

### Security Best Practices Implemented
1. **HMAC SHA-256** token signing
2. **Token expiration** (1 hour by default)
3. **Token revocation** support via Redis blacklist
4. **Secure password hashing** (SHA-256 with salt)
5. **Proper claim validation** (issuer, audience, lifetime)

## Troubleshooting

### Common Issues

1. **"JWT SecretKey is not configured"**
   - Make sure your `appsettings.json` has the `JwtSettings:SecretKey` configured

2. **Redis Connection Failed**
   - Ensure Redis is running on `localhost:6379`
   - Check the Redis connection string in `appsettings.json`

3. **Token Invalid After Restart**
   - This is expected - Redis tokens are ephemeral
   - Users need to login again after application restart

4. **401 Unauthorized on Protected Endpoints**
   - Verify the `Authorization: Bearer <token>` header is present
   - Check if the token has expired
   - Ensure the token wasn't revoked via logout

### Logging
The authentication system provides detailed logging. Check your application logs for:
- Login attempts
- Token validation failures
- Redis connection issues

## Production Considerations

1. **Generate a Strong JWT Secret**: Replace the default secret key
2. **Enable HTTPS**: Set `RequireHttpsMetadata = true` in JWT configuration
3. **Configure Redis Persistence**: Ensure Redis data survives restarts if needed
4. **Monitor Failed Login Attempts**: Implement rate limiting for login endpoint
5. **Use Environment Variables**: Store secrets in environment variables, not config files

## Next Steps

1. Replace the hardcoded test user with a proper user database
2. Add user registration functionality
3. Implement role-based authorization
4. Add refresh token support for longer sessions
5. Implement account lockout after failed login attempts