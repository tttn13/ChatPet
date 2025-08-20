import { useState, useEffect } from 'react';
import { ChatMessage } from '../types/chat';

const STORAGE_KEY = 'pet-assistant-chat';
const SESSION_KEY = 'pet-assistant-session';

export const useChatPersistence = () => {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [sessionId, setSessionId] = useState<string | undefined>();

  // Load data on component mount
  useEffect(() => {
    try {
      // Load messages from localStorage
      const savedMessages = localStorage.getItem(STORAGE_KEY);
      if (savedMessages) {
        const parsedMessages = JSON.parse(savedMessages);
        // Convert timestamp strings back to Date objects
        const messagesWithDates = parsedMessages.map((msg: any) => ({
          ...msg,
          timestamp: new Date(msg.timestamp)
        }));
        setMessages(messagesWithDates);
      }

      // Load session ID
      const savedSessionId = localStorage.getItem(SESSION_KEY);
      if (savedSessionId) {
        setSessionId(savedSessionId);
      }
    } catch (error) {
      console.error('Error loading chat data from localStorage:', error);
    }
  }, []);

  // Save messages whenever they change
  useEffect(() => {
    if (messages.length > 0) {
      try {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(messages));
      } catch (error) {
        console.error('Error saving messages to localStorage:', error);
      }
    }
  }, [messages]);

  // Save session ID whenever it changes
  useEffect(() => {
    if (sessionId) {
      try {
        localStorage.setItem(SESSION_KEY, sessionId);
      } catch (error) {
        console.error('Error saving session ID to localStorage:', error);
      }
    }
  }, [sessionId]);

  const addMessage = (message: ChatMessage) => {
    setMessages(prev => [...prev, message]);
  };

  const clearChat = () => {
    setMessages([]);
    setSessionId(undefined);
    try {
      localStorage.removeItem(STORAGE_KEY);
      localStorage.removeItem(SESSION_KEY);
    } catch (error) {
      console.error('Error clearing chat data from localStorage:', error);
    }
  };

  return {
    messages,
    sessionId,
    setMessages,
    setSessionId,
    addMessage,
    clearChat
  };
};