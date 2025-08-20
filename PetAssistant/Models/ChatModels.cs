using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace PetAssistant.Models;

public class ChatRequest
{
    [Required(ErrorMessage = "Message is required")]
    [StringLength(2000, MinimumLength = 1, ErrorMessage = "Message must be between 1 and 2000 characters")]
    [RegularExpression(@"^[^<>]*$", ErrorMessage = "Message contains invalid characters")]
    public string Message { get; init; } = string.Empty;

    [StringLength(100, ErrorMessage = "SessionId cannot exceed 100 characters")]
    [RegularExpression(@"^[a-zA-Z0-9\-_]*$", ErrorMessage = "SessionId contains invalid characters")]
    public string? SessionId { get; init; }
}

public record ChatResponse(
    string Response,
    string? Thinking,
    string SessionId,
    DateTime Timestamp
);

public record GroqRequest
{
    [JsonProperty("model")]
    public string Model { get; set; } = string.Empty;
    [JsonProperty("messages")]
    public List<GroqMessage> Messages { get; set; } = new();
}

public record GroqMessage
{
    [JsonProperty("role")]
    public string Role { get; set; } = string.Empty;
    [JsonProperty("content")]
    public string Content { get; set; } = string.Empty;
}

public record GroqResponse
{
    public string Id { get; set; } = string.Empty;
    public string Object { get; set; } = string.Empty;
    public long Created { get; set; }
    public string Model { get; set; } = string.Empty;
    public List<GroqChoice> Choices { get; set; } = new();
}

public record GroqChoice
{
    public int Index { get; set; }
    public GroqMessage Message { get; set; } = new();
    public string FinishReason { get; set; } = string.Empty;
}

public class GroqSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public bool IsThinking { get; set; } = false;
}

public record PetProfile(
    string Id,
    string Name,
    string Species,
    string? Breed,
    int? Age,
    string? Gender
);

public class CreatePetProfileRequest
{
    [Required(ErrorMessage = "Pet name is required")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Pet name must be between 1 and 50 characters")]
    [RegularExpression(@"^[a-zA-Z0-9\s\-']+$", ErrorMessage = "Pet name contains invalid characters")]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = "Species is required")]
    [StringLength(30, ErrorMessage = "Species cannot exceed 30 characters")]
    public string Species { get; init; } = string.Empty;

    [StringLength(50, ErrorMessage = "Breed cannot exceed 50 characters")]
    [RegularExpression(@"^[a-zA-Z0-9\s\-']*$", ErrorMessage = "Breed contains invalid characters")]
    public string? Breed { get; init; }

    [Range(0, 50, ErrorMessage = "Age must be between 0 and 50")]
    public int? Age { get; init; }

    [StringLength(20, ErrorMessage = "Gender cannot exceed 20 characters")]
    public string? Gender { get; init; }
}

public class UpdatePetProfileRequest
{
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Pet name must be between 1 and 50 characters")]
    [RegularExpression(@"^[a-zA-Z0-9\s\-']+$", ErrorMessage = "Pet name contains invalid characters")]
    public string? Name { get; init; }

    [StringLength(30, ErrorMessage = "Species cannot exceed 30 characters")]
    public string? Species { get; init; }

    [StringLength(50, ErrorMessage = "Breed cannot exceed 50 characters")]
    [RegularExpression(@"^[a-zA-Z0-9\s\-']*$", ErrorMessage = "Breed contains invalid characters")]
    public string? Breed { get; init; }

    [Range(0, 50, ErrorMessage = "Age must be between 0 and 50")]
    public int? Age { get; init; }

    [StringLength(20, ErrorMessage = "Gender cannot exceed 20 characters")]
    public string? Gender { get; init; }
}