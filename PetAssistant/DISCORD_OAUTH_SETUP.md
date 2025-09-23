# Discord OAuth Integration Setup

## Prerequisites

1. **Create Discord Application**
   - Go to https://discord.com/developers/applications
   - Click "New Application"
   - Give it a name (e.g., "ChatPet")
   - Navigate to "OAuth2" section

2. **Configure OAuth2 Settings**
   - Add redirect URI: `https://localhost:7001/api/auth/discord/callback`
   - Copy your Client ID and Client Secret

## Configuration

Update your `appsettings.json`:

```json
{
  "Discord": {
    "ClientId": "YOUR_DISCORD_CLIENT_ID",
    "ClientSecret": "YOUR_DISCORD_CLIENT_SECRET",
    "RedirectUri": "https://localhost:7001/api/auth/discord/callback"
  }
}
```

## API Endpoints

### 1. Initiate Discord Login
```
GET /api/auth/discord
```
Redirects to Discord OAuth authorization page.

### 2. Discord OAuth Callback
```
GET /api/auth/discord/callback?code={authorization_code}
```
Handles the Discord callback and generates JWT token.

**Response:**
```json
{
  "username": "discord_username",
  "expiresAt": "2024-01-01T12:00:00.000Z",
  "provider": "discord"
}
```

## Authentication Flow

1. User clicks "Login with Discord" → `GET /api/auth/discord`
2. User is redirected to Discord OAuth page
3. User authorizes your application
4. Discord redirects to `/api/auth/discord/callback?code=...`
5. Your backend exchanges code for Discord access token
6. Your backend fetches Discord user info
7. Your backend generates JWT token and sets cookie
8. User is authenticated and can access protected endpoints

## How It Works

- Discord users get a JWT token just like regular users
- Discord user ID is prefixed with "discord-" in your system
- Protected endpoints work the same way with `[Authorize]`
- JWT tokens are stored in Redis for revocation support

## Testing

Use the `test-discord-auth.http` file to test the endpoints:

1. Start your application with `dotnet run`
2. Visit `https://localhost:7001/api/auth/discord` in browser
3. Complete Discord OAuth flow
4. Use returned JWT token for API calls

## Production Setup

For production deployment:

1. Update redirect URI to your production domain
2. Add production domain to Discord application settings
3. Store Client ID/Secret in environment variables
4. Update CORS settings for your frontend domain

## Frontend Integration

To integrate with your React frontend:

```javascript
const loginWithDiscord = () => {
  window.location.href = 'https://your-api-domain.com/api/auth/discord';
};
```

The callback will set an HTTP-only cookie, so subsequent API calls will be automatically authenticated.