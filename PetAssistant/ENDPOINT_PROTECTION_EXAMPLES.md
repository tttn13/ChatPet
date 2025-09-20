# How to Protect Existing Endpoints with Authentication

This document shows practical examples of how to add authentication to your existing Pet Assistant API endpoints.

## Method 1: Protect Individual Endpoints

Add the `[Authorize]` attribute to specific endpoints:

```csharp
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

// In your PetProfileEndpoints.cs
group.MapGet("/{sessionId}", [Authorize] async (
    string sessionId, 
    IPetProfileService petProfileService,
    ClaimsPrincipal user) =>  // Add ClaimsPrincipal to access user info
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var username = user.FindFirst(ClaimTypes.Name)?.Value;
    
    // Now you can associate the profile with the authenticated user
    var profile = await petProfileService.GetProfileAsync(sessionId, userId);
    return profile != null ? Results.Ok(profile) : Results.NotFound();
})
.WithName("GetPetProfile")
.WithOpenApi()
.WithSummary("Get pet profile for a session");
```

## Method 2: Protect Entire Endpoint Groups

Protect all endpoints in a group at once:

### Example 1: Protect All Pet Profile Endpoints
```csharp
// In Program.cs, modify this line:
api.MapGroup("/pet-profile")
   .RequireAuthorization()  // This protects ALL endpoints in this group
   .MapPetProfileEndpoints();
```

### Example 2: Protect All Chat Endpoints
```csharp
// In Program.cs, modify this line:
api.MapGroup("/chat")
   .RequireAuthorization()  // This protects ALL endpoints in this group
   .MapChatEndpoints();
```

## Method 3: Mixed Public and Protected Endpoints

Some endpoints public, some protected in the same group:

```csharp
public static RouteGroupBuilder MapPetProfileEndpoints(this RouteGroupBuilder group)
{
    // Public endpoint - no auth required
    group.MapGet("/public-info", async () =>
    {
        return Results.Ok(new { message = "Public pet care tips" });
    });

    // Protected endpoint - requires authentication
    group.MapGet("/{sessionId}", [Authorize] async (
        string sessionId, 
        IPetProfileService petProfileService,
        ClaimsPrincipal user) =>
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var profile = await petProfileService.GetProfileAsync(sessionId, userId);
        return profile != null ? Results.Ok(profile) : Results.NotFound();
    });

    // Another protected endpoint
    group.MapPost("/{sessionId}", [Authorize] async (
        string sessionId,
        CreatePetProfileRequest request,
        IPetProfileService petProfileService,
        IValidationService validationService,
        ClaimsPrincipal user) =>
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        // Your existing logic here, but now associated with the user
        return Results.Created($"/api/pet-profile/{sessionId}", result);
    });
}
```

## Quick Protection for Your Current Endpoints

### Option A: Protect Everything (Recommended for secure APIs)
Update your `Program.cs`:

```csharp
// Protect all API endpoints
var api = app.MapGroup("/api")
    .RequireAuthorization()  // This protects ALL /api/* endpoints
    .WithOpenApi();

// These are now all protected:
api.MapGroup("/chat")
   .MapChatEndpoints();

api.MapGroup("/pet-profile")
   .MapPetProfileEndpoints();

// Auth endpoints stay unprotected (they handle their own auth)
api.MapGroup("/auth")
   .AllowAnonymous()  // Explicitly allow anonymous access
   .MapAuthEndpoints();
```

### Option B: Selective Protection
Keep some endpoints public, protect others:

```csharp
var api = app.MapGroup("/api")
    .WithOpenApi();

// Public chat endpoint (maybe for demo purposes)
api.MapGroup("/chat")
   .MapChatEndpoints();

// Protected pet profiles (user-specific data)
api.MapGroup("/pet-profile")
   .RequireAuthorization()
   .MapPetProfileEndpoints();

// Auth endpoints (public by design)
api.MapGroup("/auth")
   .MapAuthEndpoints();
```

## Getting User Information in Protected Endpoints

When an endpoint is protected with `[Authorize]`, you can access user information:

```csharp
group.MapPost("/create-profile", [Authorize] async (
    CreatePetProfileRequest request,
    IPetProfileService petProfileService,
    ClaimsPrincipal user) =>
{
    // Get user information from JWT token
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var username = user.FindFirst(ClaimTypes.Name)?.Value;
    
    // Use this information in your business logic
    var profile = new PetProfile
    {
        OwnerId = userId,
        OwnerName = username,
        PetName = request.PetName,
        // ... other properties
    };

    await petProfileService.CreateProfileAsync(profile);
    return Results.Created($"/api/pet-profile/{profile.Id}", profile);
});
```

## Testing Your Protected Endpoints

### 1. Without Authentication (Should Fail)
```bash
curl -X GET "https://localhost:7001/api/pet-profile/test-session"
# Should return: 401 Unauthorized
```

### 2. With Authentication (Should Work)
```bash
# First login
curl -X POST "https://localhost:7001/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username": "testuser", "password": "passwordTest@!"}'

# Use the token from login response
curl -X GET "https://localhost:7001/api/pet-profile/test-session" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE"
```

## Common Authorization Patterns

### Role-Based Authorization (Future Enhancement)
```csharp
// You can add roles to JWT tokens and use them like this:
group.MapDelete("/{id}", [Authorize(Roles = "Admin")] async (string id) =>
{
    // Only users with Admin role can access this
});
```

### Policy-Based Authorization (Future Enhancement)
```csharp
// Define custom policies for more complex authorization
group.MapGet("/sensitive-data", [Authorize(Policy = "RequirePetOwner")] async () =>
{
    // Only users matching the custom policy can access this
});
```

## Error Responses for Protected Endpoints

When authentication fails, your endpoints will automatically return:

- **401 Unauthorized**: No token provided or invalid token
- **403 Forbidden**: Valid token but insufficient permissions (when using roles/policies)

The authentication middleware handles these responses automatically.

## Summary

To quickly secure your API:

1. **For maximum security**: Add `.RequireAuthorization()` to your main API group
2. **For selective security**: Add `[Authorize]` to individual endpoints that need protection
3. **Always test**: Verify that protected endpoints return 401 without authentication and work with valid tokens
4. **Use user info**: Access `ClaimsPrincipal user` parameter to get authenticated user details

Remember: The authentication endpoints (`/api/auth/*`) should NOT be protected, as they handle authentication themselves.