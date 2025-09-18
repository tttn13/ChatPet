import apiClient from "../config/api";
import { ChatResponse } from "../types/chat";

const chatApiService = {
    async fetchResponse(message: string, sessionId: string): Promise<ChatResponse> {
        try {
            const response = await apiClient.post<ChatResponse>('/api/chat', {
                message,
                sessionId,
            });

            return response.data;
        } catch (error) {
            console.error('Request failed:', error);
            throw error;
        }
    },
}

export { chatApiService };