# Pet Assistant API

A ChatGPT-like API for pet health consultations powered by Groq AI.

## Features

- AI-powered pet health advice using Groq's LLaMA model
- Conversation history management
- RESTful API with Swagger documentation
- CORS enabled for frontend integration

## Setup

1. **Add your Groq API key**
   - Edit `appsettings.json`
   - Replace `YOUR_GROQ_API_KEY_HERE` with your actual Groq API key

2. **Run the application**
   ```bash
   dotnet run
   ```

3. **Access the API**
   - API: http://localhost:5293
   - Swagger UI: http://localhost:5293/swagger

## API Endpoints

### POST /api/chat
Send a message to the pet health assistant.

Request body:
```json
{
  "message": "My dog is scratching a lot, what could be the cause?",
  "sessionId": "optional-session-id"
}
```

Response:
```json
{
  "response": "Excessive scratching in dogs can have several causes...",
  "sessionId": "session-id",
  "timestamp": "2025-08-13T12:00:00Z"
}
```

### GET /api/health
Health check endpoint.

## Configuration

The Groq settings in `appsettings.json`:
- `ApiKey`: Your Groq API key
- `Model`: The AI model to use (default: llama-3.3-70b-versatile)
- `MaxTokens`: Maximum response length
- `Temperature`: Response creativity (0-1)

## Security Note

Never commit your actual API key to version control. Consider using:
- Environment variables
- User secrets (for development)
- Azure Key Vault or similar (for production)