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

export interface PetProfile {
  id: string;
  name: string;
  species: string;
  breed?: string;
  age?: number;
  gender?: string;
}

export interface CreatePetProfileRequest {
  name: string;
  species: string;
  breed?: string;
  age?: number;
  gender?: string;
}

export interface UpdatePetProfileRequest {
  name?: string;
  species?: string;
  breed?: string;
  age?: number;
  gender?: string;
}