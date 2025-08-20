using PetAssistant.Models;

namespace PetAssistant.Services;

public interface IPetProfileService
{
    Task<PetProfile?> GetProfileAsync(string sessionId);
    Task<PetProfile> CreateProfileAsync(string sessionId, CreatePetProfileRequest request);
    Task<PetProfile?> UpdateProfileAsync(string sessionId, UpdatePetProfileRequest request);
    Task<bool> DeleteProfileAsync(string sessionId);
}

public class PetProfileService : IPetProfileService
{
    private readonly Dictionary<string, PetProfile> _profiles = new();
    private readonly ILogger<PetProfileService> _logger;

    public PetProfileService(ILogger<PetProfileService> logger)
    {
        _logger = logger;
    }

    public Task<PetProfile?> GetProfileAsync(string sessionId)
    {
        _profiles.TryGetValue(sessionId, out var profile);
        _logger.LogInformation("Retrieved profile for session {SessionId}: {Found}", sessionId, profile != null);
        return Task.FromResult(profile);
    }

    public Task<PetProfile> CreateProfileAsync(string sessionId, CreatePetProfileRequest request)
    {
        var profile = new PetProfile(
            Id: Guid.NewGuid().ToString(),
            Name: request.Name,
            Species: request.Species,
            Breed: request.Breed,
            Age: request.Age,
            Gender: request.Gender
        );

        _profiles[sessionId] = profile;
        _logger.LogInformation("Created profile for session {SessionId}: {ProfileId}", sessionId, profile.Id);
        return Task.FromResult(profile);
    }

    public Task<PetProfile?> UpdateProfileAsync(string sessionId, UpdatePetProfileRequest request)
    {
        if (!_profiles.TryGetValue(sessionId, out var existingProfile))
        {
            _logger.LogWarning("Profile not found for session {SessionId}", sessionId);
            return Task.FromResult<PetProfile?>(null);
        }

        var updatedProfile = existingProfile with
        {
            Name = request.Name ?? existingProfile.Name,
            Species = request.Species ?? existingProfile.Species,
            Breed = request.Breed ?? existingProfile.Breed,
            Age = request.Age ?? existingProfile.Age,
            Gender = request.Gender ?? existingProfile.Gender
        };

        _profiles[sessionId] = updatedProfile;
        _logger.LogInformation("Updated profile for session {SessionId}", sessionId);
        return Task.FromResult<PetProfile?>(updatedProfile);
    }

    public Task<bool> DeleteProfileAsync(string sessionId)
    {
        var removed = _profiles.Remove(sessionId);
        _logger.LogInformation("Deleted profile for session {SessionId}: {Success}", sessionId, removed);
        return Task.FromResult(removed);
    }
}