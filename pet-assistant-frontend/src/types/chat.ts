export interface ChatMessage {
  id: string;
  message: string;
  response: string;
  thinking?: string | null;
  timestamp: Date;
  isUser: boolean;
}

export interface ChatResponse {
  response: string;
  thinking: string | null;
  sessionId: string;
  timestamp: string;
}

export interface ChatRequest {
  message: string;
  sessionId?: string;
}