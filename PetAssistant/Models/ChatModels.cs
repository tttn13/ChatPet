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


