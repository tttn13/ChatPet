import { useContext } from 'react';
import AuthContext from '../contexts/AuthContext';

/**
 * Hook to access authentication context
 * Throws error if used outside of AuthProvider
 */
export const useAuth = () => {
  const context = useContext(AuthContext);
  
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  
  return context;
};

/**
 * Hook to check if user is authenticated
 * Returns boolean without throwing errors
 */
export const useIsAuthenticated = (): boolean => {
  const context = useContext(AuthContext);
  return context?.isAuthenticated || false;
};

/**
 * Hook to get current user
 * Returns null if not authenticated
 */
export const useCurrentUser = () => {
  const context = useContext(AuthContext);
  return context?.user || null;
};

export default useAuth;