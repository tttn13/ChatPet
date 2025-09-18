using Newtonsoft.Json;

using PetAssistant.Services;
// GroqRequest is a DTO that deserialize JSON and mapped the JSON field names to c# property names 
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

public abstract record ApiResponse;
public record Success(GroqResponse Response) : ApiResponse;
public record Error(ErrorResponse Response) : ApiResponse;