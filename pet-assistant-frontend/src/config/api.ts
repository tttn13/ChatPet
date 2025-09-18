import axios, { AxiosError } from 'axios';

const API_URL = process.env.REACT_APP_API_URL || 'http://localhost:5293';

const apiClient = axios.create({
  baseURL: API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true, // This ensures cookies are sent automatically
  // Allow self-signed certificates in development
  // In production, you should use proper SSL certificates
  ...(process.env.NODE_ENV === 'development' && {
    httpsAgent: {
      rejectUnauthorized: false
    }
  })
});

// No request interceptor needed - cookies are sent automatically!

// Response interceptor to handle 401 errors
apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    if (error.response?.status === 401) {
      // Cookie has expired or been revoked
      // Only redirect if not already on login page
      if (window.location.pathname !== '/login') {
        window.location.href = '/login';
      }
    }
    
    return Promise.reject(error);
  }
);

export default apiClient;
export { API_URL };