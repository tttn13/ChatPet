using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PetAssistant.Models;
using PetAssistant.Services;
using Xunit;

namespace PetAssistant.Tests.Services;

public class PetProfileServiceTests
{
    private readonly Mock<ILogger<PetProfileService>> _mockLogger;
    private readonly PetProfileService _sut;

    public PetProfileServiceTests()
    {
        _mockLogger = new Mock<ILogger<PetProfileService>>();
        _sut = new PetProfileService(_mockLogger.Object);
    }

    #region GetProfileAsync Tests

    [Fact]
    public async Task GetProfileAsync_ExistingProfile_ReturnsProfile()
    {
        // Arrange
        var sessionId = "test-session-123";
        var createRequest = new CreatePetProfileRequest
        {
            Name = "Max",
            Species = "Dog",
            Breed = "Labrador",
            Age = 3,
            Gender = "Male"
        };

        // Create a profile first
        await _sut.CreateProfileAsync(sessionId, createRequest);

        // Act
        var result = await _sut.GetProfileAsync(sessionId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Max");
        result.Species.Should().Be("Dog");
        result.Breed.Should().Be("Labrador");
        result.Age.Should().Be(3);
        result.Gender.Should().Be("Male");
        result.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetProfileAsync_NonExistingProfile_ReturnsNull()
    {
        // Arrange
        var sessionId = "non-existing-session";

        // Act
        var result = await _sut.GetProfileAsync(sessionId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProfileAsync_AfterDeletion_ReturnsNull()
    {
        // Arrange
        var sessionId = "test-session";
        var createRequest = new CreatePetProfileRequest
        {
            Name = "Buddy",
            Species = "Cat"
        };

        await _sut.CreateProfileAsync(sessionId, createRequest);
        await _sut.DeleteProfileAsync(sessionId);

        // Act
        var result = await _sut.GetProfileAsync(sessionId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateProfileAsync Tests

    [Fact]
    public async Task CreateProfileAsync_ValidRequest_CreatesProfileWithUniqueId()
    {
        // Arrange
        var sessionId = "test-session";
        var request = new CreatePetProfileRequest
        {
            Name = "Luna",
            Species = "Cat",
            Breed = "Persian",
            Age = 2,
            Gender = "Female"
        };

        // Act
        var result = await _sut.CreateProfileAsync(sessionId, request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();
        result.Id.Should().HaveLength(36); // GUID length with hyphens
        result.Name.Should().Be("Luna");
        result.Species.Should().Be("Cat");
        result.Breed.Should().Be("Persian");
        result.Age.Should().Be(2);
        result.Gender.Should().Be("Female");
    }

    [Fact]
    public async Task CreateProfileAsync_MinimalRequest_CreatesProfileWithNullOptionalFields()
    {
        // Arrange
        var sessionId = "test-session";
        var request = new CreatePetProfileRequest
        {
            Name = "Birdie",
            Species = "Bird"
        };

        // Act
        var result = await _sut.CreateProfileAsync(sessionId, request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Birdie");
        result.Species.Should().Be("Bird");
        result.Breed.Should().BeNull();
        result.Age.Should().BeNull();
        result.Gender.Should().BeNull();
    }

    [Fact]
    public async Task CreateProfileAsync_DuplicateSession_OverwritesExistingProfile()
    {
        // Arrange
        var sessionId = "duplicate-session";
        var firstRequest = new CreatePetProfileRequest
        {
            Name = "First Pet",
            Species = "Dog"
        };
        var secondRequest = new CreatePetProfileRequest
        {
            Name = "Second Pet",
            Species = "Cat"
        };

        // Act
        var firstProfile = await _sut.CreateProfileAsync(sessionId, firstRequest);
        var secondProfile = await _sut.CreateProfileAsync(sessionId, secondRequest);
        var retrievedProfile = await _sut.GetProfileAsync(sessionId);

        // Assert
        firstProfile.Name.Should().Be("First Pet");
        secondProfile.Name.Should().Be("Second Pet");
        retrievedProfile!.Name.Should().Be("Second Pet");
        retrievedProfile.Species.Should().Be("Cat");
        firstProfile.Id.Should().NotBe(secondProfile.Id); // Different IDs
    }

    [Fact]
    public async Task CreateProfileAsync_MultipleSessions_CreatesSeparateProfiles()
    {
        // Arrange
        var session1 = "session-1";
        var session2 = "session-2";
        var request1 = new CreatePetProfileRequest
        {
            Name = "Pet1",
            Species = "Dog"
        };
        var request2 = new CreatePetProfileRequest
        {
            Name = "Pet2",
            Species = "Cat"
        };

        // Act
        await _sut.CreateProfileAsync(session1, request1);
        await _sut.CreateProfileAsync(session2, request2);
        var profile1 = await _sut.GetProfileAsync(session1);
        var profile2 = await _sut.GetProfileAsync(session2);

        // Assert
        profile1!.Name.Should().Be("Pet1");
        profile2!.Name.Should().Be("Pet2");
        profile1.Id.Should().NotBe(profile2.Id);
    }

    #endregion

    #region UpdateProfileAsync Tests

    [Fact]
    public async Task UpdateProfileAsync_ExistingProfile_UpdatesSuccessfully()
    {
        // Arrange
        var sessionId = "update-session";
        var createRequest = new CreatePetProfileRequest
        {
            Name = "Original Name",
            Species = "Dog",
            Age = 1
        };
        var updateRequest = new UpdatePetProfileRequest
        {
            Name = "Updated Name",
            Age = 2
        };

        await _sut.CreateProfileAsync(sessionId, createRequest);

        // Act
        var result = await _sut.UpdateProfileAsync(sessionId, updateRequest);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Name");
        result.Species.Should().Be("Dog"); // Unchanged
        result.Age.Should().Be(2);
    }

    [Fact]
    public async Task UpdateProfileAsync_NonExistingProfile_ReturnsNull()
    {
        // Arrange
        var sessionId = "non-existing";
        var updateRequest = new UpdatePetProfileRequest
        {
            Name = "New Name"
        };

        // Act
        var result = await _sut.UpdateProfileAsync(sessionId, updateRequest);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateProfileAsync_PartialUpdate_OnlyUpdatesProvidedFields()
    {
        // Arrange
        var sessionId = "partial-update";
        var createRequest = new CreatePetProfileRequest
        {
            Name = "Max",
            Species = "Dog",
            Breed = "Labrador",
            Age = 3,
            Gender = "Male"
        };
        var updateRequest = new UpdatePetProfileRequest
        {
            Age = 4
            // Only updating age, all other fields should remain unchanged
        };

        var originalProfile = await _sut.CreateProfileAsync(sessionId, createRequest);

        // Act
        var result = await _sut.UpdateProfileAsync(sessionId, updateRequest);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(originalProfile.Id); // ID remains the same
        result.Name.Should().Be("Max");
        result.Species.Should().Be("Dog");
        result.Breed.Should().Be("Labrador");
        result.Age.Should().Be(4); // Only this changed
        result.Gender.Should().Be("Male");
    }

    [Fact]
    public async Task UpdateProfileAsync_EmptyUpdate_ReturnsUnchangedProfile()
    {
        // Arrange
        var sessionId = "empty-update";
        var createRequest = new CreatePetProfileRequest
        {
            Name = "Unchanged",
            Species = "Cat",
            Age = 5
        };
        var updateRequest = new UpdatePetProfileRequest(); // Empty update

        var originalProfile = await _sut.CreateProfileAsync(sessionId, createRequest);

        // Act
        var result = await _sut.UpdateProfileAsync(sessionId, updateRequest);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Unchanged");
        result.Species.Should().Be("Cat");
        result.Age.Should().Be(5);
        result.Should().BeEquivalentTo(originalProfile);
    }

    [Fact]
    public async Task UpdateProfileAsync_NullValues_PreservesExistingValues()
    {
        // Arrange
        var sessionId = "null-update";
        var createRequest = new CreatePetProfileRequest
        {
            Name = "Pet",
            Species = "Dog",
            Breed = "Poodle",
            Age = 2,
            Gender = "Female"
        };
        var updateRequest = new UpdatePetProfileRequest
        {
            Breed = null,
            Age = null,
            Gender = null
        };

        await _sut.CreateProfileAsync(sessionId, createRequest);

        // Act
        var result = await _sut.UpdateProfileAsync(sessionId, updateRequest);

        // Assert - null values in update preserve existing values (using ?? operator)
        result.Should().NotBeNull();
        result!.Name.Should().Be("Pet");
        result.Species.Should().Be("Dog");
        result.Breed.Should().Be("Poodle"); // Preserved due to ?? operator
        result.Age.Should().Be(2); // Preserved due to ?? operator  
        result.Gender.Should().Be("Female"); // Preserved due to ?? operator
    }

    #endregion

    #region DeleteProfileAsync Tests

    [Fact]
    public async Task DeleteProfileAsync_ExistingProfile_ReturnsTrue()
    {
        // Arrange
        var sessionId = "delete-session";
        var createRequest = new CreatePetProfileRequest
        {
            Name = "ToDelete",
            Species = "Cat"
        };

        await _sut.CreateProfileAsync(sessionId, createRequest);

        // Act
        var result = await _sut.DeleteProfileAsync(sessionId);

        // Assert
        result.Should().BeTrue();
        var profile = await _sut.GetProfileAsync(sessionId);
        profile.Should().BeNull();
    }

    [Fact]
    public async Task DeleteProfileAsync_NonExistingProfile_ReturnsFalse()
    {
        // Arrange
        var sessionId = "non-existing-delete";

        // Act
        var result = await _sut.DeleteProfileAsync(sessionId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteProfileAsync_DeleteTwice_SecondReturnsFalse()
    {
        // Arrange
        var sessionId = "double-delete";
        var createRequest = new CreatePetProfileRequest
        {
            Name = "Pet",
            Species = "Dog"
        };

        await _sut.CreateProfileAsync(sessionId, createRequest);

        // Act
        var firstDelete = await _sut.DeleteProfileAsync(sessionId);
        var secondDelete = await _sut.DeleteProfileAsync(sessionId);

        // Assert
        firstDelete.Should().BeTrue();
        secondDelete.Should().BeFalse();
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task GetProfileAsync_LogsInformation()
    {
        // Arrange
        var sessionId = "log-test";

        // Act
        await _sut.GetProfileAsync(sessionId);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retrieved profile")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateProfileAsync_LogsCreation()
    {
        // Arrange
        var sessionId = "create-log-test";
        var request = new CreatePetProfileRequest
        {
            Name = "Test",
            Species = "Cat"
        };

        // Act
        await _sut.CreateProfileAsync(sessionId, request);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Created profile")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateProfileAsync_NonExisting_LogsWarning()
    {
        // Arrange
        var sessionId = "warning-log-test";
        var request = new UpdatePetProfileRequest { Name = "Test" };

        // Act
        await _sut.UpdateProfileAsync(sessionId, request);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Profile not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Concurrency Tests

    [Fact]
    public async Task CreateProfileAsync_ConcurrentCreates_HandlesCorrectly()
    {
        // Arrange
        var tasks = new List<Task<PetProfile>>();
        
        for (int i = 0; i < 10; i++)
        {
            var sessionId = $"concurrent-{i}";
            var request = new CreatePetProfileRequest
            {
                Name = $"Pet{i}",
                Species = i % 2 == 0 ? "Dog" : "Cat"
            };
            
            tasks.Add(_sut.CreateProfileAsync(sessionId, request));
        }

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(10);
        results.Select(r => r.Id).Should().OnlyHaveUniqueItems();
        results.Select(r => r.Name).Should().OnlyHaveUniqueItems();
    }

    #endregion
}