import { useState, useEffect } from 'react';
import { PetProfile, CreatePetProfileRequest } from '../types/chat';

const API_URL = process.env.REACT_APP_API_URL || 'http://localhost:5293';
const STORAGE_KEY = 'petProfile';

export const usePetProfile = (sessionId?: string) => {
  const [profile, setProfile] = useState<PetProfile | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Load from localStorage on mount
  useEffect(() => {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored) {
      try {
        setProfile(JSON.parse(stored));
      } catch (e) {
        console.error('Failed to parse stored pet profile:', e);
      }
    }
  }, []);

  // Save to localStorage whenever profile changes
  useEffect(() => {
    if (profile) {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(profile));
    } else {
      localStorage.removeItem(STORAGE_KEY);
    }
  }, [profile]);

  // Fetch profile from backend when sessionId is available
  useEffect(() => {
    if (sessionId) {
      fetchProfile(sessionId);
    }
  }, [sessionId]);

  const fetchProfile = async (sid: string) => {
    setIsLoading(true);
    setError(null);
    try {
      const response = await fetch(`${API_URL}/api/pet-profile/${sid}`);
      if (response.ok) {
        const data = await response.json();
        setProfile(data);
      } else if (response.status !== 404) {
        throw new Error('Failed to fetch profile');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
    } finally {
      setIsLoading(false);
    }
  };

  const createProfile = async (data: CreatePetProfileRequest) => {
    if (!sessionId) {
      setError('No session ID available');
      return;
    }

    setIsLoading(true);
    setError(null);
    try {
      const response = await fetch(`${API_URL}/api/pet-profile/${sessionId}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
      });

      if (!response.ok) {
        throw new Error('Failed to create profile');
      }

      const newProfile = await response.json();
      setProfile(newProfile);
      return newProfile;
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
      throw err;
    } finally {
      setIsLoading(false);
    }
  };

  const updateProfile = async (data: Partial<CreatePetProfileRequest>) => {
    if (!sessionId) {
      setError('No session ID available');
      return;
    }

    setIsLoading(true);
    setError(null);
    try {
      const response = await fetch(`${API_URL}/api/pet-profile/${sessionId}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
      });

      if (!response.ok) {
        throw new Error('Failed to update profile');
      }

      const updatedProfile = await response.json();
      setProfile(updatedProfile);
      return updatedProfile;
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
      throw err;
    } finally {
      setIsLoading(false);
    }
  };

  const deleteProfile = async () => {
    if (!sessionId) {
      setError('No session ID available');
      return;
    }

    setIsLoading(true);
    setError(null);
    try {
      const response = await fetch(`${API_URL}/api/pet-profile/${sessionId}`, {
        method: 'DELETE',
      });

      if (!response.ok) {
        throw new Error('Failed to delete profile');
      }

      setProfile(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
      throw err;
    } finally {
      setIsLoading(false);
    }
  };

  return {
    profile,
    isLoading,
    error,
    createProfile,
    updateProfile,
    deleteProfile,
  };
};