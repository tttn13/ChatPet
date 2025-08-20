using PetAssistant.Models;
using PetAssistant.Services;

namespace PetAssistant.Endpoints;

public static class PetProfileEndpoints
{
    public static RouteGroupBuilder MapPetProfileEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{sessionId}", async (string sessionId, IPetProfileService petProfileService) =>
        {
            var profile = await petProfileService.GetProfileAsync(sessionId);
            return profile != null ? Results.Ok(profile) : Results.NotFound();
        })
        .WithName("GetPetProfile")
        .WithOpenApi()
        .WithSummary("Get pet profile for a session");

        group.MapPost("/{sessionId}", async (
            string sessionId,
            CreatePetProfileRequest request,
            IPetProfileService petProfileService,
            IValidationService validationService) =>
        {
            // Validate business rules
            var validationResult = validationService.ValidateCreatePetProfile(request);
            if (!validationResult.IsValid)
            {
                return Results.BadRequest(new { errors = validationResult.Errors });
            }

            var profile = await petProfileService.CreateProfileAsync(sessionId, request);
            return Results.Ok(profile);
        })
        .WithName("CreatePetProfile")
        .WithOpenApi()
        .WithSummary("Create a new pet profile");

        group.MapPut("/{sessionId}", async (
            string sessionId,
            UpdatePetProfileRequest request,
            IPetProfileService petProfileService,
            IValidationService validationService) =>
        {
            // Validate business rules
            var validationResult = validationService.ValidateUpdatePetProfile(request);
            if (!validationResult.IsValid)
            {
                return Results.BadRequest(new { errors = validationResult.Errors });
            }

            var profile = await petProfileService.UpdateProfileAsync(sessionId, request);
            return profile != null ? Results.Ok(profile) : Results.NotFound();
        })
        .WithName("UpdatePetProfile")
        .WithOpenApi()
        .WithSummary("Update an existing pet profile");

        group.MapDelete("/{sessionId}", async (string sessionId, IPetProfileService petProfileService) =>
        {
            var deleted = await petProfileService.DeleteProfileAsync(sessionId);
            return deleted ? Results.Ok() : Results.NotFound();
        })
        .WithName("DeletePetProfile")
        .WithOpenApi()
        .WithSummary("Delete a pet profile");

        return group;
    }
}