using Ganss.Xss;

namespace PetAssistant.Services;

public interface IValidationService
{
   string SanitizeInput(string input);
}

public class ValidationService : IValidationService
{
    private readonly string[] _validSpecies = { "Dog", "Cat", "Bird", "Rabbit", "Hamster", "Guinea Pig", "Fish", "Other" };
    private readonly string[] _validGenders = { "Male", "Female", "Neutered Male", "Spayed Female" };
    private readonly HtmlSanitizer _sanitizer;

    public ValidationService()
    {
        _sanitizer = new HtmlSanitizer();
        // strips out ALL HTML content, leaving only plain text.
        _sanitizer.AllowedTags.Clear();
        _sanitizer.AllowedAttributes.Clear();
        _sanitizer.AllowedCssProperties.Clear();
        _sanitizer.AllowedSchemes.Clear();
    }

    public string SanitizeInput(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var sanitized = _sanitizer.Sanitize(input);

        return sanitized.Trim();
    }
}

public record ValidationResult(bool IsValid, List<string> Errors);