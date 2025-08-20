# Pet Assistant Frontend

A React-based chat interface for the Pet Assistant API, providing veterinary advice and pet care information.

## Features

- 💬 Real-time chat interface
- 🧠 Thinking process display for AI responses
- 📱 Responsive design with Tailwind CSS
- 📚 Component documentation with Storybook
- 🎨 Modern, clean UI

## Prerequisites

- Node.js 16+
- Pet Assistant API running on http://localhost:5293

## Installation

```bash
npm install
```

## Running the Application

### Start the Frontend
```bash
npm start
```
The app will run on http://localhost:3000

### Start Storybook
```bash
npm run storybook
```
Storybook will run on http://localhost:6006

## Environment Variables

Create a `.env` file in the root directory:

```env
REACT_APP_API_URL=http://localhost:5293
```

## Project Structure

```
src/
├── components/        # React components
│   ├── ChatContainer.tsx
│   ├── ChatMessage.tsx
│   └── ChatInput.tsx
├── stories/          # Storybook stories
│   ├── ChatContainer.stories.tsx
│   ├── ChatMessage.stories.tsx
│   └── ChatInput.stories.tsx
├── types/            # TypeScript type definitions
│   └── chat.ts
└── App.tsx          # Main application component
```

## Available Scripts

- `npm start` - Run the app in development mode
- `npm test` - Run tests
- `npm run build` - Build for production
- `npm run storybook` - Run Storybook
- `npm run build-storybook` - Build Storybook

## Technologies Used

- React 19 with TypeScript
- Tailwind CSS for styling
- Storybook for component documentation
- Fetch API for backend communication