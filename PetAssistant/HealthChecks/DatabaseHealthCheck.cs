using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PetAssistant.HealthChecks;

/// <summary>
/// Concrete implementation #2: Database Health Check
/// Shows how different health checks can share the base functionality
/// </summary>
public class DatabaseHealthCheck : BaseHealthCheck
{
    private readonly IPetProfileService _petProfileService;

    public DatabaseHealthCheck(IPetProfileService petProfileService, ILogger<DatabaseHealthCheck> logger) 
        : base(logger)
    {
        _petProfileService = petProfileService;
    }

    protected override async Task<HealthCheckResult> CheckHealthCore(CancellationToken cancellationToken)
    {
        // Test database connectivity by trying to read a profile
        var testSessionId = "health_check_" + Guid.NewGuid().ToString("N");
        
        try
        {
            // Try to create and retrieve a test profile
            var testProfile = new CreatePetProfileRequest
            {
                Name = "HealthCheckPet",
                Species = "Dog",
                Age = 1
            };
            
            // Create profile
            var created = await _petProfileService.CreateProfileAsync(testSessionId, testProfile);
            
            // Retrieve it
            var retrieved = await _petProfileService.GetProfileAsync(testSessionId);
            
            // Clean up
            await _petProfileService.DeleteProfileAsync(testSessionId);
            
            if (retrieved != null && retrieved.Name == "HealthCheckPet")
            {
                return HealthCheckResult.Healthy(
                    "Database operations working",
                    new Dictionary<string, object>
                    {
                        ["operation"] = "CRUD test successful"
                    });
            }
            
            return HealthCheckResult.Degraded("Database test partially failed");
        }
        catch (Exception)
        {
            // Don't throw - let base class handle this
            throw;
        }
    }
}