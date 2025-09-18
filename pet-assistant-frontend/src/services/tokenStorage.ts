/**
 * Token storage service - manages JWT token in localStorage
 * Tokens expire after 1 hour (configured on backend)
 */
const TOKEN_KEY = 'token';
const TOKEN_EXPIRATION = 'token_expiry'
const tokenStorage = {
  getToken: (): string | null => {
    return localStorage.getItem(TOKEN_KEY);
  },
  
  getTokenExpiration: (): string | null => {
    return localStorage.getItem(TOKEN_EXPIRATION);
  },

  setToken: (token: string, expiry: string): void => {
    localStorage.setItem(TOKEN_KEY, token);
    localStorage.setItem(TOKEN_EXPIRATION, expiry);
  },

  removeToken: (): void => {
    localStorage.removeItem(TOKEN_KEY);
  },

  hasToken: (): boolean => {
    return localStorage.getItem(TOKEN_KEY) !== null;
  }
};

export default tokenStorage;