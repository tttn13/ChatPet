import apiClient from '../config/api';

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  username: string;
  expiresAt: string;
  tokenType: string;
}

export interface User {
  username: string;
  userId: string;
  isAuthenticated: boolean;
}

export interface ValidateTokenResponse {
  valid: boolean;
  username: string;
  expiresAt: string;
}

const authService = {
  /**
   * Login with username and password
   */
  async login(username: string, password: string): Promise<LoginResponse> {
    try {
      const response = await apiClient.post<LoginResponse>('/api/auth/login', {
        username,
        password,
      });
      
      // Cookie is automatically set by the server
      // No need to store token anywhere
      
      return response.data;
    } catch (error) {
      console.error('Login failed:', error);
      throw error;
    }
  },

  /**
   * Logout the current user
   */
  async logout(): Promise<void> {
    try {
      // Call logout endpoint to invalidate token on server
      // This will also clear the cookie
      await apiClient.post('/api/auth/logout');
    } catch (error) {
      console.error('Logout error:', error);
      // Cookie will be cleared by server response
    }
  },

  /**
   * Get current user information
   */
  async getCurrentUser(): Promise<User | null> {
    try {
      //cookie will be sent automatically
      const response = await apiClient.get<User>('/api/auth/me');
      return response.data;
    } catch (error) {
      console.error('Failed to get current user:', error);
      return null;
    }
  },

  /**
   * Validate the current token
   */
  async validateToken(): Promise<boolean> {
    try {
      // Just try to validate - cookie will be sent automatically
      const response = await apiClient.get<ValidateTokenResponse>('/api/auth/validate');
      return response.data.valid;
    } catch (error) {
      console.error('Token validation failed:', error);
      return false;
    }
  },

  /**
   * Check if user is authenticated
   * Note: With HTTP-only cookies, we can't check locally
   * Must call the validate endpoint
   */
  async isAuthenticated(): Promise<boolean> {
    return await this.validateToken();
  },

  /**
   * Initiate Discord OAuth login
   */
  loginWithDiscord(): void {
    window.location.href = `${process.env.REACT_APP_API_URL || 'http://localhost:5293'}/api/auth/discord`;
  },
  /**
   * Initiate Google OAuth login
   */
  loginWithGoogle(): void {
    window.location.href = `${process.env.REACT_APP_API_URL || 'http://localhost:5293'}/api/auth/google`;
  },
};

export default authService;