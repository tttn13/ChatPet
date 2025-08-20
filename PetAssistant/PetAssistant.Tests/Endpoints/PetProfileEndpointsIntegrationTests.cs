using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PetAssistant.Models;
using PetAssistant.Services;
using PetAssistant.Tests.Helpers;
using Xunit;

namespace PetAssistant.Tests.Endpoints;

public class PetProfileEndpointsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PetProfileEndpointsIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetProfile_ExistingProfile_ReturnsOk()
    {
        // Arrange
        var sessionId = "test-session-123";
        var expectedProfile = new PetProfile("pet123", "Buddy", "Dog", "Labrador", 3, "Male");
        
        var mockPetProfileService = TestMockFactory.CreatePetProfileService(expectedProfile);
        mockPetProfileService.Setup(x => x.GetProfileAsync(sessionId))
            .ReturnsAsync(expectedProfile);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                RemoveService<IPetProfileService>(services);
                services.AddSingleton(mockPetProfileService.Object);
            });
        }).CreateClient();

        // Act
        var response = await client.GetAsync($"/api/pet-profile/{sessionId}");
        var profile = await response.Content.ReadFromJsonAsync<PetProfile>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        profile.Should().NotBeNull();
        profile!.Name.Should().Be("Buddy");
        profile.Species.Should().Be("Dog");
        profile.Breed.Should().Be("Labrador");
        profile.Age.Should().Be(3);
        profile.Gender.Should().Be("Male");
    }

    [Fact]
    public async Task GetProfile_NonExistingProfile_ReturnsNotFound()
    {
        // Arrange
        var sessionId = "non-existing-session";
        
        var mockPetProfileService = new Mock<IPetProfileService>();
        mockPetProfileService.Setup(x => x.GetProfileAsync(sessionId))
            .ReturnsAsync((PetProfile?)null);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                RemoveService<IPetProfileService>(services);
                services.AddSingleton(mockPetProfileService.Object);
            });
        }).CreateClient();

        // Act
        var response = await client.GetAsync($"/api/pet-profile/{sessionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateProfile_ValidRequest_ReturnsCreatedProfile()
    {
        // Arrange
        var sessionId = "new-session-123";
        var request = new CreatePetProfileRequest
        {
            Name = "Luna",
            Species = "Cat",
            Breed = "Persian",
            Age = 2,
            Gender = "Female"
        };
        
        var expectedProfile = new PetProfile("pet456", "Luna", "Cat", "Persian", 2, "Female");
        
        var mockPetProfileService = new Mock<IPetProfileService>();
        var mockValidationService = new Mock<IValidationService>();
        
        mockPetProfileService.Setup(x => x.CreateProfileAsync(sessionId, It.IsAny<CreatePetProfileRequest>()))
            .ReturnsAsync(expectedProfile);
        
        mockValidationService.Setup(x => x.ValidateCreatePetProfile(It.IsAny<CreatePetProfileRequest>()))
            .Returns(new ValidationResult(true, new List<string>()));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                RemoveService<IPetProfileService>(services);
                RemoveService<IValidationService>(services);
                services.AddSingleton(mockPetProfileService.Object);
                services.AddSingleton(mockValidationService.Object);
            });
        }).CreateClient();

        // Act
        var response = await client.PostAsJsonAsync($"/api/pet-profile/{sessionId}", request);
        var profile = await response.Content.ReadFromJsonAsync<PetProfile>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        profile.Should().NotBeNull();
        profile!.Name.Should().Be("Luna");
        profile.Species.Should().Be("Cat");
        profile.Breed.Should().Be("Persian");
        
        mockValidationService.Verify(x => x.ValidateCreatePetProfile(It.IsAny<CreatePetProfileRequest>()), Times.Once);
        mockPetProfileService.Verify(x => x.CreateProfileAsync(It.IsAny<string>(), It.IsAny<CreatePetProfileRequest>()), Times.Once);
    }

    [Fact]
    public async Task CreateProfile_InvalidSpecies_ReturnsBadRequest()
    {
        // Arrange
        var sessionId = "test-session";
        var request = new CreatePetProfileRequest
        {
            Name = "Rex",
            Species = "Dinosaur", // Invalid species
            Age = 5
        };
        
        var mockPetProfileService = new Mock<IPetProfileService>();
        var mockValidationService = new Mock<IValidationService>();
        
        mockValidationService.Setup(x => x.ValidateCreatePetProfile(It.IsAny<CreatePetProfileRequest>()))
            .Returns(new ValidationResult(false, 
                new List<string> { "Invalid species. Allowed species are: Dog, Cat, Bird, Fish, Rabbit, Hamster, Guinea Pig, Ferret, Reptile, Other" }));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                RemoveService<IPetProfileService>(services);
                RemoveService<IValidationService>(services);
                services.AddSingleton(mockPetProfileService.Object);
                services.AddSingleton(mockValidationService.Object);
            });
        }).CreateClient();

        // Act
        var response = await client.PostAsJsonAsync($"/api/pet-profile/{sessionId}", request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        content.Should().Contain("Invalid species");
        
        mockValidationService.Verify(x => x.ValidateCreatePetProfile(It.IsAny<CreatePetProfileRequest>()), Times.Once);
        // PetProfileService should not be called when validation fails
    }

    [Fact]
    public async Task CreateProfile_MissingRequiredFields_ReturnsBadRequest()
    {
        // Arrange
        var sessionId = "test-session";
        var request = new CreatePetProfileRequest
        {
            Name = "", // Missing required name
            Species = "" // Missing required species
        };
        
        var mockPetProfileService = new Mock<IPetProfileService>();
        var mockValidationService = new Mock<IValidationService>();
        
        mockValidationService.Setup(x => x.ValidateCreatePetProfile(It.IsAny<CreatePetProfileRequest>()))
            .Returns(new ValidationResult(false, 
                new List<string> { "Pet name is required", "Species is required" }));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                RemoveService<IPetProfileService>(services);
                RemoveService<IValidationService>(services);
                services.AddSingleton(mockPetProfileService.Object);
                services.AddSingleton(mockValidationService.Object);
            });
        }).CreateClient();

        // Act
        var response = await client.PostAsJsonAsync($"/api/pet-profile/{sessionId}", request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        content.Should().Contain("Pet name is required");
        content.Should().Contain("Species is required");
    }

    [Fact]
    public async Task UpdateProfile_ExistingProfile_ReturnsUpdated()
    {
        // Arrange
        var sessionId = "existing-session";
        var request = new UpdatePetProfileRequest
        {
            Name = "Buddy Updated",
            Age = 4
        };
        
        var updatedProfile = new PetProfile("pet123", "Buddy Updated", "Dog", "Labrador", 4, "Male");
        
        var mockPetProfileService = new Mock<IPetProfileService>();
        var mockValidationService = new Mock<IValidationService>();
        
        mockValidationService.Setup(x => x.ValidateUpdatePetProfile(It.IsAny<UpdatePetProfileRequest>()))
            .Returns(new ValidationResult(true, new List<string>()));
        
        mockPetProfileService.Setup(x => x.UpdateProfileAsync(It.IsAny<string>(), It.IsAny<UpdatePetProfileRequest>()))
            .ReturnsAsync(updatedProfile);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                RemoveService<IPetProfileService>(services);
                RemoveService<IValidationService>(services);
                services.AddSingleton(mockPetProfileService.Object);
                services.AddSingleton(mockValidationService.Object);
            });
        }).CreateClient();

        // Act
        var response = await client.PutAsJsonAsync($"/api/pet-profile/{sessionId}", request);
        var profile = await response.Content.ReadFromJsonAsync<PetProfile>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        profile.Should().NotBeNull();
        profile!.Name.Should().Be("Buddy Updated");
        profile.Age.Should().Be(4);
        
        mockValidationService.Verify(x => x.ValidateUpdatePetProfile(It.IsAny<UpdatePetProfileRequest>()), Times.Once);
        mockPetProfileService.Verify(x => x.UpdateProfileAsync(It.IsAny<string>(), It.IsAny<UpdatePetProfileRequest>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProfile_NonExistingProfile_ReturnsNotFound()
    {
        // Arrange
        var sessionId = "non-existing-session";
        var request = new UpdatePetProfileRequest
        {
            Name = "Updated Name"
        };
        
        var mockPetProfileService = new Mock<IPetProfileService>();
        var mockValidationService = new Mock<IValidationService>();
        
        mockValidationService.Setup(x => x.ValidateUpdatePetProfile(It.IsAny<UpdatePetProfileRequest>()))
            .Returns(new ValidationResult(true, new List<string>()));
        
        mockPetProfileService.Setup(x => x.UpdateProfileAsync(It.IsAny<string>(), It.IsAny<UpdatePetProfileRequest>()))
            .ReturnsAsync((PetProfile?)null);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                RemoveService<IPetProfileService>(services);
                RemoveService<IValidationService>(services);
                services.AddSingleton(mockPetProfileService.Object);
                services.AddSingleton(mockValidationService.Object);
            });
        }).CreateClient();

        // Act
        var response = await client.PutAsJsonAsync($"/api/pet-profile/{sessionId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateProfile_InvalidData_ReturnsBadRequest()
    {
        // Arrange
        var sessionId = "test-session";
        var request = new UpdatePetProfileRequest
        {
            Age = 100 // Invalid age - exceeds max of 50
        };
        
        var mockPetProfileService = new Mock<IPetProfileService>();
        var mockValidationService = new Mock<IValidationService>();
        
        mockValidationService.Setup(x => x.ValidateUpdatePetProfile(It.IsAny<UpdatePetProfileRequest>()))
            .Returns(new ValidationResult(false, 
                new List<string> { "Age must be between 0 and 50" }));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                RemoveService<IPetProfileService>(services);
                RemoveService<IValidationService>(services);
                services.AddSingleton(mockPetProfileService.Object);
                services.AddSingleton(mockValidationService.Object);
            });
        }).CreateClient();

        // Act
        var response = await client.PutAsJsonAsync($"/api/pet-profile/{sessionId}", request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        content.Should().Contain("Age must be between 0 and 50");
    }

    [Fact]
    public async Task DeleteProfile_ExistingProfile_ReturnsOk()
    {
        // Arrange
        var sessionId = "existing-session";
        
        var mockPetProfileService = new Mock<IPetProfileService>();
        mockPetProfileService.Setup(x => x.DeleteProfileAsync(sessionId))
            .ReturnsAsync(true);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                RemoveService<IPetProfileService>(services);
                services.AddSingleton(mockPetProfileService.Object);
            });
        }).CreateClient();

        // Act
        var response = await client.DeleteAsync($"/api/pet-profile/{sessionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        mockPetProfileService.Verify(x => x.DeleteProfileAsync(sessionId), Times.Once);
    }

    [Fact]
    public async Task DeleteProfile_NonExistingProfile_ReturnsNotFound()
    {
        // Arrange
        var sessionId = "non-existing-session";
        
        var mockPetProfileService = new Mock<IPetProfileService>();
        mockPetProfileService.Setup(x => x.DeleteProfileAsync(sessionId))
            .ReturnsAsync(false);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                RemoveService<IPetProfileService>(services);
                services.AddSingleton(mockPetProfileService.Object);
            });
        }).CreateClient();

        // Act
        var response = await client.DeleteAsync($"/api/pet-profile/{sessionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        mockPetProfileService.Verify(x => x.DeleteProfileAsync(sessionId), Times.Once);
    }

    [Fact]
    public async Task CreateProfile_MaliciousInput_SanitizedByValidation()
    {
        // Arrange
        var sessionId = "security-test";
        var request = new CreatePetProfileRequest
        {
            Name = "<script>alert('xss')</script>Fluffy",
            Species = "Cat",
            Breed = "SELECT * FROM pets"
        };
        
        var mockPetProfileService = new Mock<IPetProfileService>();
        var mockValidationService = new Mock<IValidationService>();
        
        mockValidationService.Setup(x => x.ValidateCreatePetProfile(It.IsAny<CreatePetProfileRequest>()))
            .Returns(new ValidationResult(false, 
                new List<string> { "Pet name contains invalid characters", "Breed contains invalid characters" }));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                RemoveService<IPetProfileService>(services);
                RemoveService<IValidationService>(services);
                services.AddSingleton(mockPetProfileService.Object);
                services.AddSingleton(mockValidationService.Object);
            });
        }).CreateClient();

        // Act
        var response = await client.PostAsJsonAsync($"/api/pet-profile/{sessionId}", request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        content.Should().Contain("invalid characters");
        
        mockValidationService.Verify(x => x.ValidateCreatePetProfile(It.IsAny<CreatePetProfileRequest>()), Times.Once);
        // PetProfileService should not be called when validation fails
    }

    // Helper method to remove a service from the service collection
    private static void RemoveService<TService>(IServiceCollection services)
    {
        var descriptors = services.Where(d => d.ServiceType == typeof(TService)).ToList();
        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }
    }
}