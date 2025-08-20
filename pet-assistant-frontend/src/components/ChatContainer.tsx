import React, { useState, useRef, useEffect } from 'react';
import { ChatMessage } from './ChatMessage';
import { ChatInput } from './ChatInput';
import { ChatActions } from './ChatActions';
import { ChatMessage as ChatMessageType, ChatResponse } from '../types/chat';
import { useChatPersistence } from '../hooks/useChatPersistence';

const API_URL = process.env.REACT_APP_API_URL || 'http://localhost:5293';

export const ChatContainer: React.FC = () => {
  const {
    messages,
    sessionId,
    setSessionId,
    addMessage,
    clearChat
  } = useChatPersistence();
  
  const [isLoading, setIsLoading] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages]);

  const sendMessage = async (message: string) => {
    // Add user message
    const userMessage: ChatMessageType = {
      id: Date.now().toString(),
      message,
      response: '',
      timestamp: new Date(),
      isUser: true,
    };
    addMessage(userMessage);
    setIsLoading(true);

    try {
      const response = await fetch(`${API_URL}/api/chat`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          message,
          sessionId,
        }),
      });

      if (!response.ok) {
        throw new Error('Failed to get response');
      }

      const data: ChatResponse = await response.json();
      
      // Update sessionId if not set
      if (!sessionId) {
        setSessionId(data.sessionId);
      }

      // Add assistant response
      const assistantMessage: ChatMessageType = {
        id: (Date.now() + 1).toString(),
        message: '',
        response: data.response,
        thinking: data.thinking,
        timestamp: new Date(data.timestamp),
        isUser: false,
      };
      addMessage(assistantMessage);
    } catch (error) {
      console.error('Error sending message:', error);
      // Add error message
      const errorMessage: ChatMessageType = {
        id: (Date.now() + 1).toString(),
        message: '',
        response: 'Sorry, I encountered an error. Please try again later.',
        timestamp: new Date(),
        isUser: false,
      };
      addMessage(errorMessage);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="flex flex-col h-screen bg-gray-50">
      <header className="bg-white shadow-sm border-b border-gray-200 px-6 py-4">
        <div className="max-w-4xl mx-auto flex justify-between items-center">
          <div>
            <h1 className="text-2xl font-bold text-gray-800">🐾 Pet Assistant</h1>
            <p className="text-sm text-gray-600">Your virtual veterinary companion</p>
          </div>
          <ChatActions
            onClearChat={clearChat}
            messagesCount={messages.length}
          />
        </div>
      </header>

      <div className="flex-1 overflow-y-auto px-6 py-4">
        <div className="max-w-4xl mx-auto">
          {messages.length === 0 ? (
            <div className="text-center py-12">
              <div className="text-6xl mb-4">🐶🐱🐰</div>
              <h2 className="text-xl font-semibold text-gray-700 mb-2">
                Welcome to Pet Assistant!
              </h2>
              <p className="text-gray-600">
                Ask me anything about your pet's health, behavior, nutrition, or general care.
              </p>
              <div className="mt-6 grid grid-cols-1 md:grid-cols-2 gap-3 max-w-2xl mx-auto">
                <button
                  onClick={() => sendMessage("My cat won't eat. What should I do?")}
                  className="text-left p-3 bg-white rounded-lg shadow hover:shadow-md transition-shadow"
                >
                  <div className="font-medium text-gray-700">🐱 Eating Issues</div>
                  <div className="text-sm text-gray-500">My cat won't eat...</div>
                </button>
                <button
                  onClick={() => sendMessage("How often should I walk my dog?")}
                  className="text-left p-3 bg-white rounded-lg shadow hover:shadow-md transition-shadow"
                >
                  <div className="font-medium text-gray-700">🐕 Exercise</div>
                  <div className="text-sm text-gray-500">How often to walk...</div>
                </button>
                <button
                  onClick={() => sendMessage("What vaccines does my puppy need?")}
                  className="text-left p-3 bg-white rounded-lg shadow hover:shadow-md transition-shadow"
                >
                  <div className="font-medium text-gray-700">💉 Vaccinations</div>
                  <div className="text-sm text-gray-500">Puppy vaccine schedule...</div>
                </button>
                <button
                  onClick={() => sendMessage("How do I introduce a new pet to my home?")}
                  className="text-left p-3 bg-white rounded-lg shadow hover:shadow-md transition-shadow"
                >
                  <div className="font-medium text-gray-700">🏠 New Pet</div>
                  <div className="text-sm text-gray-500">Introducing new pets...</div>
                </button>
              </div>
            </div>
          ) : (
            <>
              {messages.map((message) => (
                <ChatMessage key={message.id} message={message} />
              ))}
              {isLoading && (
                <div className="flex justify-start mb-4">
                  <div className="bg-gray-100 rounded-lg px-4 py-3">
                    <div className="flex items-center gap-2">
                      <div className="animate-bounce">🐾</div>
                      <div className="animate-pulse">Pet Assistant is thinking...</div>
                    </div>
                  </div>
                </div>
              )}
              <div ref={messagesEndRef} />
            </>
          )}
        </div>
      </div>

      <ChatInput onSendMessage={sendMessage} isLoading={isLoading} />
    </div>
  );
};