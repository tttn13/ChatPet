import React from 'react';
import { Link } from 'react-router-dom';
import { PetProfile } from '../components/PetProfile';
import { usePetProfile } from '../hooks/usePetProfile';
import { useChatPersistence } from '../hooks/useChatPersistence';

export const PetProfilePage: React.FC = () => {
  const { sessionId } = useChatPersistence();
  const {
    profile,
    createProfile,
    updateProfile,
    deleteProfile,
    isLoading
  } = usePetProfile(sessionId);

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-white shadow-sm border-b border-gray-200 px-6 py-4">
        <div className="max-w-4xl mx-auto flex justify-between items-center">
          <div>
            <h1 className="text-2xl font-bold text-gray-800">🐾 Pet Profile</h1>
            <p className="text-sm text-gray-600">Manage your pet's information</p>
          </div>
          <Link
            to="/"
            className="px-4 py-2 text-blue-600 hover:bg-blue-50 rounded-lg transition-colors flex items-center gap-2"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 19l-7-7m0 0l7-7m-7 7h18" />
            </svg>
            Back to Chat
          </Link>
        </div>
      </header>

      <div className="max-w-4xl mx-auto px-6 py-8">
        <div className="bg-white rounded-lg shadow-lg p-6">
          <PetProfile
            profile={profile}
            onCreateProfile={createProfile}
            onUpdateProfile={updateProfile}
            onDeleteProfile={deleteProfile}
            isLoading={isLoading}
          />
        </div>

        {profile && (
          <div className="mt-6 bg-blue-50 border border-blue-200 rounded-lg p-4">
            <h3 className="font-semibold text-blue-900 mb-2">Profile Active</h3>
            <p className="text-blue-700 text-sm">
              Your pet's profile is now active. The AI assistant will personalize responses based on {profile.name}'s information.
            </p>
          </div>
        )}
      </div>
    </div>
  );
};