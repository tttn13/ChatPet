# ChatPet - AI-Powered Pet Health Assistant

ChatPet is a full-stack application that provides an AI-powered virtual assistant for pet health questions and guidance. Users can ask questions about their pets' health, behavior, and care, receiving intelligent responses powered by AI.

## Features

- **AI-Powered Responses**: Intelligent pet health advice using Groq AI
- **Real-time Chat Interface**: Modern chat UI with message history
- **User Authentication**: Secure JWT-based authentication system
- **Conversation Persistence**: Redis-backed conversation storage
- **Rate Limiting**: Built-in API rate limiting for protection
- **Responsive Design**: Tailwind CSS-based responsive interface

## Tech Stack

### Backend (.NET 8)
- **Framework**: ASP.NET Core 8.0 with Minimal APIs
- **Authentication**: JWT Bearer tokens
- **Database**: Redis for session and conversation storage
- **AI Integration**: Groq AI API for intelligent responses
- **Testing**: xUnit with integration tests
- **Rate Limiting**: AspNetCoreRateLimit
- **Documentation**: Swagger/OpenAPI

### Frontend (React + TypeScript)
- **Framework**: React 19 with TypeScript
- **Styling**: Tailwind CSS
- **Routing**: React Router v7
- **HTTP Client**: Axios
- **Markdown Rendering**: react-markdown with GitHub Flavored Markdown
- **Testing**: Jest + React Testing Library
- **Component Development**: Storybook

## Getting Started

### Prerequisites

- Node.js 16+ and npm
- .NET 8 SDK
- Redis server
- Groq API key

### Backend Setup

1. Navigate to the backend directory:
```bash
cd PetAssistant
```

2. Configure your environment variables in `appsettings.json` or user secrets:
```json
{
  "Groq": {
    "ApiKey": "your-groq-api-key"
  },
  "Jwt": {
    "SecretKey": "your-jwt-secret-key",
    "Issuer": "PetAssistant",
    "Audience": "PetAssistantClient"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```

3. Restore dependencies and run:
```bash
dotnet restore
dotnet run
```

The API will be available at `https://localhost:7193` (or `http://localhost:5193`)

### Frontend Setup

1. Navigate to the frontend directory:
```bash
cd pet-assistant-frontend
```

2. Install dependencies:
```bash
npm install
```

3. Configure the API endpoint in `src/config/api.ts` if needed

4. Start the development server:
```bash
npm start
```

The application will open at `http://localhost:3000`

## Available Scripts

### Backend
- `dotnet run` - Start the API server
- `dotnet test` - Run unit and integration tests
- `dotnet build` - Build the project

### Frontend
- `npm start` - Start development server
- `npm test` - Run tests
- `npm run build` - Build for production
- `npm run storybook` - Launch Storybook for component development

## API Endpoints

- `GET /api/health` - Health check endpoint
- `POST /api/auth/login` - User authentication
- `POST /api/auth/logout` - User logout
- `POST /api/chat/message` - Send chat message (requires authentication)
- `GET /api/chat/conversation/{conversationId}` - Get conversation history (requires authentication)

## Project Structure

```
ChatPet/
├── PetAssistant/                 # .NET Backend
│   ├── Endpoints/               # API endpoint definitions
│   ├── Services/                # Business logic and services
│   ├── Models/                  # Data models and DTOs
│   ├── Extensions/              # Service extensions
│   └── PetAssistant.Tests/      # Unit and integration tests
│
└── pet-assistant-frontend/      # React Frontend
    ├── src/
    │   ├── components/          # React components
    │   ├── contexts/            # React contexts
    │   ├── hooks/               # Custom React hooks
    │   ├── pages/               # Page components
    │   ├── services/            # API services
    │   ├── stories/             # Storybook stories
    │   └── types/               # TypeScript type definitions
    └── public/                  # Static assets
```

## Testing

### Backend Tests
```bash
cd PetAssistant
dotnet test
```

### Frontend Tests
```bash
cd pet-assistant-frontend
npm test
```

## Deployment

The application is configured for deployment on:
- **Backend**: Render.com (or any .NET-compatible hosting)
- **Frontend**: Vercel (or any static hosting service)

### Environment Variables for Production

Backend:
- `Groq__ApiKey`
- `Jwt__SecretKey`
- `Redis__ConnectionString`

Frontend:
- `REACT_APP_API_URL` (API endpoint URL)

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License.

## Support

For issues and questions, please open an issue in the GitHub repository.