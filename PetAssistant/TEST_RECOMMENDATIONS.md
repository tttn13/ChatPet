# Unit Test Recommendations for PetAssistant Backend

## Overview
Based on my analysis of your PetAssistant backend code, here's a comprehensive unit testing strategy. The project already has xUnit, Moq, FluentAssertions, and Microsoft.AspNetCore.Mvc.Testing configured.

## Test Structure

### 1. Service Layer Tests

#### **ValidationServiceTests**
- `ValidateCreatePetProfile_ValidInput_ReturnsValid()`
- `ValidateCreatePetProfile_InvalidSpecies_ReturnsErrors()`
- `ValidateCreatePetProfile_InvalidGender_ReturnsErrors()`
- `ValidateCreatePetProfile_MaliciousContent_ReturnsErrors()`
- `ValidateUpdatePetProfile_ValidInput_ReturnsValid()`
- `ValidateUpdatePetProfile_InvalidSpecies_ReturnsErrors()`
- `SanitizeInput_RemovesHtmlTags()`
- `SanitizeInput_RemovesMaliciousPatterns()`
- `SanitizeInput_TrimsWhitespace()`

#### **PetProfileServiceTests**
- `GetProfileAsync_ExistingProfile_ReturnsProfile()`
- `GetProfileAsync_NonExistingProfile_ReturnsNull()`
- `CreateProfileAsync_ValidRequest_CreatesProfile()`
- `CreateProfileAsync_DuplicateSession_OverwritesExisting()`
- `UpdateProfileAsync_ExistingProfile_UpdatesSuccessfully()`
- `UpdateProfileAsync_NonExistingProfile_ReturnsNull()`
- `UpdateProfileAsync_PartialUpdate_OnlyUpdatesProvidedFields()`
- `DeleteProfileAsync_ExistingProfile_ReturnsTrue()`
- `DeleteProfileAsync_NonExistingProfile_ReturnsFalse()`

#### **CacheServiceTests**
- `GetCachedResponseAsync_CacheMiss_ReturnsNull()`
- `GetCachedResponseAsync_CacheHit_ReturnsResponse()`
- `SetCachedResponseAsync_StoresResponse()`
- `SetCachedResponseAsync_SameKeyDifferentProfile_CreatesDifferentEntry()`
- `ClearCache_RemovesAllEntries()`
- `GetStatistics_ReturnsAccurateMetrics()`
- `CacheExpiration_OldEntriesExpire()`
- `NormalizeMessage_StandardizesInput()`

#### **ErrorHandlingServiceTests**
- `HandleGroqApiError_BadRequest_ReturnsAppropriateMessage()`
- `HandleGroqApiError_Unauthorized_ReturnsAuthMessage()`
- `HandleGroqApiError_TooManyRequests_ReturnsRateLimitMessage()`
- `HandleGroqApiError_InternalServerError_ReturnsServerErrorMessage()`
- `HandleGroqApiError_UnknownStatus_ReturnsGenericMessage()`
- `HandleGenericError_HttpRequestException_ReturnsNetworkError()`
- `HandleGenericError_TaskCanceledException_ReturnsTimeoutError()`
- `HandleGenericError_UnknownException_ReturnsGenericError()`

#### **TelemetryServiceTests**
- `RecordApiCall_IncrementsTotalCalls()`
- `RecordApiCall_Failure_IncrementsFailedCalls()`
- `RecordCacheHit_UpdatesStatistics()`
- `RecordGroqApiCall_TracksErrors()`
- `RecordGroqApiCall_TracksErrorCodes()`
- `GetStats_ReturnsAccurateMetrics()`
- `GetStats_CalculatesCorrectRatios()`

#### **GroqServiceTests** (Complex - Requires extensive mocking)
- `GetPetAdviceAsync_NewSession_CreatesSession()`
- `GetPetAdviceAsync_ExistingSession_MaintainsHistory()`
- `GetPetAdviceAsync_WithPetProfile_IncludesInSystemPrompt()`
- `GetPetAdviceAsync_CacheHit_ReturnsCachedResponse()`
- `GetPetAdviceAsync_ApiError_HandlesGracefully()`
- `GetPetAdviceAsync_ThinkingModel_ParsesThinkingContent()`
- `GetPetAdviceAsync_LongConversation_TrimsOldMessages()`
- `GetPetAdviceAsync_HttpTimeout_ReturnsErrorMessage()`
- `GetPetAdviceAsync_InvalidApiKey_ReturnsAuthError()`

#### **ConversationCleanupServiceTests**
- `CleanupOldSessions_RemovesExpiredSessions()`
- `CleanupOldSessions_KeepsActiveSessions()`
- `UpdateSessionActivity_UpdatesMetadata()`
- `GetExpiredSessions_ReturnsCorrectList()`
- `GetStats_ReturnsAccurateMetrics()`
- `BackgroundService_RunsPeriodically()`

### 2. Endpoint Integration Tests

#### **ChatEndpointsTests**
- `PostChat_ValidRequest_ReturnsOk()`
- `PostChat_EmptyMessage_ReturnsBadRequest()`
- `PostChat_LongMessage_TruncatesOrRejects()`
- `PostChat_WithSessionId_MaintainsSession()`
- `PostChat_WithPetProfile_UsesProfile()`
- `PostChat_MaliciousInput_Sanitizes()`
- `PostChat_ServiceError_Returns500()`

#### **PetProfileEndpointsTests**
- `GetProfile_ExistingProfile_ReturnsOk()`
- `GetProfile_NonExistingProfile_ReturnsNotFound()`
- `CreateProfile_ValidRequest_ReturnsCreatedProfile()`
- `CreateProfile_InvalidSpecies_ReturnsBadRequest()`
- `CreateProfile_MissingRequiredFields_ReturnsBadRequest()`
- `UpdateProfile_ExistingProfile_ReturnsUpdated()`
- `UpdateProfile_NonExistingProfile_ReturnsNotFound()`
- `DeleteProfile_ExistingProfile_ReturnsOk()`
- `DeleteProfile_NonExistingProfile_ReturnsNotFound()`

#### **HealthEndpointsTests**
- `HealthCheck_ReturnsHealthy()`
- `DetailedHealthCheck_IncludesAllChecks()`
- `Metrics_ReturnsStatistics()`
- `Metrics_IncludesAllServices()`

### 3. Middleware Tests

#### **SecurityMiddlewareTests**
- `InvokeAsync_AddsSecurityHeaders()`
- `InvokeAsync_LargeRequest_Returns413()`
- `InvokeAsync_SuspiciousPath_Returns400()`
- `InvokeAsync_NormalRequest_PassesThrough()`
- `ContainsSuspiciousPatterns_DetectsPathTraversal()`
- `ContainsSuspiciousPatterns_DetectsScriptInjection()`

### 4. Health Check Tests

#### **GroqApiHealthCheckTests**
- `CheckHealthAsync_ApiResponding_ReturnsHealthy()`
- `CheckHealthAsync_NetworkError_ReturnsUnhealthy()`
- `CheckHealthAsync_Timeout_ReturnsUnhealthy()`
- `CheckHealthAsync_UnexpectedStatus_ReturnsDegraded()`

### 5. Model Validation Tests

#### **ChatRequestValidationTests**
- `Message_Required_ValidationFails()`
- `Message_TooLong_ValidationFails()`
- `Message_InvalidCharacters_ValidationFails()`
- `SessionId_InvalidCharacters_ValidationFails()`

#### **PetProfileRequestValidationTests**
- `Name_Required_ValidationFails()`
- `Name_InvalidCharacters_ValidationFails()`
- `Species_Required_ValidationFails()`
- `Age_OutOfRange_ValidationFails()`
- `Breed_TooLong_ValidationFails()`

## Test Implementation Priorities

### High Priority (Core Business Logic)
1. GroqService tests (core functionality)
2. ValidationService tests (security/data integrity)
3. PetProfileService tests (user data management)
4. Chat endpoint integration tests

### Medium Priority (Supporting Services)
1. CacheService tests (performance)
2. ErrorHandlingService tests (user experience)
3. PetProfile endpoint integration tests
4. SecurityMiddleware tests

### Low Priority (Monitoring/Health)
1. TelemetryService tests
2. ConversationCleanupService tests
3. Health endpoint tests
4. GroqApiHealthCheck tests

## Testing Best Practices

### 1. Use Test Data Builders
```csharp
public class PetProfileBuilder
{
    private string _name = "Buddy";
    private string _species = "Dog";
    // ... builder pattern for test data
}
```

### 2. Mock External Dependencies
```csharp
var mockHttpClient = new Mock<HttpClient>();
var mockLogger = new Mock<ILogger<GroqService>>();
var mockCache = new Mock<ICacheService>();
```

### 3. Use FluentAssertions for Readable Tests
```csharp
result.Should().NotBeNull();
result.IsValid.Should().BeTrue();
result.Errors.Should().BeEmpty();
```

### 4. Test Edge Cases
- Null inputs
- Empty strings
- Maximum length inputs
- Concurrent operations
- Timeout scenarios

### 5. Integration Test Setup
```csharp
public class ApiTestFixture : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Override services with test doubles
    }
}
```

## Code Coverage Goals
- **Target**: 80% overall coverage
- **Critical paths**: 90%+ (GroqService, ValidationService)
- **Controllers/Endpoints**: 85%+
- **Utility services**: 70%+

## Testing Tools Configuration

### Required NuGet Packages (Already Included)
- xunit
- Moq
- FluentAssertions
- Microsoft.AspNetCore.Mvc.Testing
- coverlet.collector

### Additional Recommended Packages
```xml
<PackageReference Include="Bogus" Version="35.0.1" /> <!-- Fake data generation -->
<PackageReference Include="WireMock.Net" Version="1.5.40" /> <!-- HTTP mocking -->
<PackageReference Include="Respawn" Version="6.1.0" /> <!-- Database cleanup for integration tests -->
```

## Sample Test Structure

```csharp
public class ValidationServiceTests
{
    private readonly ValidationService _sut;

    public ValidationServiceTests()
    {
        _sut = new ValidationService();
    }

    [Fact]
    public void SanitizeInput_RemovesScriptTags()
    {
        // Arrange
        var input = "Hello <script>alert('xss')</script> World";
        
        // Act
        var result = _sut.SanitizeInput(input);
        
        // Assert
        result.Should().NotContain("<script>");
        result.Should().NotContain("</script>");
    }

    [Theory]
    [InlineData("Cat", true)]
    [InlineData("Dinosaur", false)]
    public void ValidateCreatePetProfile_ChecksValidSpecies(string species, bool expectedValid)
    {
        // Arrange
        var request = new CreatePetProfileRequest
        {
            Name = "Test",
            Species = species
        };
        
        // Act
        var result = _sut.ValidateCreatePetProfile(request);
        
        // Assert
        result.IsValid.Should().Be(expectedValid);
    }
}
```

## Next Steps
1. Create test file structure matching the service structure
2. Start with high-priority service tests
3. Add integration tests using WebApplicationFactory
4. Set up CI/CD pipeline with test execution
5. Configure code coverage reporting
6. Add performance/load tests for critical endpoints