import React, { useState } from 'react';
import { PetProfile as PetProfileType, CreatePetProfileRequest } from '../types/chat';

interface PetProfileProps {
  profile: PetProfileType | null;
  onCreateProfile: (data: CreatePetProfileRequest) => Promise<void>;
  onUpdateProfile: (data: Partial<CreatePetProfileRequest>) => Promise<void>;
  onDeleteProfile: () => Promise<void>;
  isLoading?: boolean;
}

export const PetProfile: React.FC<PetProfileProps> = ({
  profile,
  onCreateProfile,
  onUpdateProfile,
  onDeleteProfile,
  isLoading = false,
}) => {
  const [isEditing, setIsEditing] = useState(false);
  const [formData, setFormData] = useState<CreatePetProfileRequest>({
    name: profile?.name || '',
    species: profile?.species || '',
    breed: profile?.breed || '',
    age: profile?.age || undefined,
    gender: profile?.gender || '',
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (profile) {
        await onUpdateProfile(formData);
      } else {
        await onCreateProfile(formData);
      }
      setIsEditing(false);
    } catch (error) {
      console.error('Failed to save profile:', error);
    }
  };

  const handleEdit = () => {
    setFormData({
      name: profile?.name || '',
      species: profile?.species || '',
      breed: profile?.breed || '',
      age: profile?.age || undefined,
      gender: profile?.gender || '',
    });
    setIsEditing(true);
  };

  const handleDelete = async () => {
    if (window.confirm('Are you sure you want to delete this pet profile?')) {
      try {
        await onDeleteProfile();
        setIsEditing(false);
      } catch (error) {
        console.error('Failed to delete profile:', error);
      }
    }
  };

  if (!profile && !isEditing) {
    return (
      <div className="bg-white rounded-lg shadow p-4 mb-4">
        <div className="text-center py-4">
          <div className="text-4xl mb-2">🐾</div>
          <h3 className="text-lg font-semibold text-gray-700 mb-2">No Pet Profile</h3>
          <p className="text-gray-500 mb-4">Create a profile to get personalized advice</p>
          <button
            onClick={() => setIsEditing(true)}
            className="px-4 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 transition-colors"
            disabled={isLoading}
          >
            Create Pet Profile
          </button>
        </div>
      </div>
    );
  }

  if (isEditing) {
    return (
      <div className="bg-white rounded-lg shadow p-4 mb-4">
        <h3 className="text-lg font-semibold text-gray-800 mb-4">
          {profile ? 'Edit Pet Profile' : 'Create Pet Profile'}
        </h3>
        <form onSubmit={handleSubmit} className="space-y-3">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Pet Name*
            </label>
            <input
              type="text"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
              required
              disabled={isLoading}
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Species*
            </label>
            <select
              value={formData.species}
              onChange={(e) => setFormData({ ...formData, species: e.target.value })}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
              required
              disabled={isLoading}
            >
              <option value="">Select species</option>
              <option value="Dog">Dog</option>
              <option value="Cat">Cat</option>
              <option value="Bird">Bird</option>
              <option value="Rabbit">Rabbit</option>
              <option value="Hamster">Hamster</option>
              <option value="Guinea Pig">Guinea Pig</option>
              <option value="Fish">Fish</option>
              <option value="Other">Other</option>
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Breed
            </label>
            <input
              type="text"
              value={formData.breed}
              onChange={(e) => setFormData({ ...formData, breed: e.target.value })}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="e.g., Golden Retriever, Persian, etc."
              disabled={isLoading}
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Age (years)
            </label>
            <input
              type="number"
              value={formData.age || ''}
              onChange={(e) => setFormData({ ...formData, age: e.target.value ? parseInt(e.target.value) : undefined })}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
              min="0"
              max="50"
              disabled={isLoading}
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Gender
            </label>
            <select
              value={formData.gender}
              onChange={(e) => setFormData({ ...formData, gender: e.target.value })}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
              disabled={isLoading}
            >
              <option value="">Select gender</option>
              <option value="Male">Male</option>
              <option value="Female">Female</option>
              <option value="Neutered Male">Neutered Male</option>
              <option value="Spayed Female">Spayed Female</option>
            </select>
          </div>

          <div className="flex gap-2 pt-2">
            <button
              type="submit"
              className="px-4 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 transition-colors"
              disabled={isLoading}
            >
              {isLoading ? 'Saving...' : 'Save'}
            </button>
            <button
              type="button"
              onClick={() => setIsEditing(false)}
              className="px-4 py-2 bg-gray-300 text-gray-700 rounded-lg hover:bg-gray-400 transition-colors"
              disabled={isLoading}
            >
              Cancel
            </button>
          </div>
        </form>
      </div>
    );
  }

  return (
    <div className="bg-white rounded-lg shadow p-4 mb-4">
      <div className="flex justify-between items-start">
        <div>
          <h3 className="text-lg font-semibold text-gray-800 mb-2">Pet Profile</h3>
          <div className="space-y-1 text-sm">
            <p><span className="font-medium text-gray-600">Name:</span> {profile!.name}</p>
            <p><span className="font-medium text-gray-600">Species:</span> {profile!.species}</p>
            {profile!.breed && (
              <p><span className="font-medium text-gray-600">Breed:</span> {profile!.breed}</p>
            )}
            {profile!.age && (
              <p><span className="font-medium text-gray-600">Age:</span> {profile!.age} years</p>
            )}
            {profile!.gender && (
              <p><span className="font-medium text-gray-600">Gender:</span> {profile!.gender}</p>
            )}
          </div>
        </div>
        <div className="flex gap-2">
          <button
            onClick={handleEdit}
            className="px-3 py-1 text-sm bg-blue-100 text-blue-700 rounded hover:bg-blue-200 transition-colors"
            disabled={isLoading}
          >
            Edit
          </button>
          <button
            onClick={handleDelete}
            className="px-3 py-1 text-sm bg-red-100 text-red-700 rounded hover:bg-red-200 transition-colors"
            disabled={isLoading}
          >
            Delete
          </button>
        </div>
      </div>
    </div>
  );
};