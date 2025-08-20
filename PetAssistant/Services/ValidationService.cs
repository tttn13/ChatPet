using PetAssistant.Models;
using System.Text.RegularExpressions;
using System.Web;

namespace PetAssistant.Services;

public interface IValidationService
{
    ValidationResult ValidateCreatePetProfile(CreatePetProfileRequest request);
    ValidationResult ValidateUpdatePetProfile(UpdatePetProfileRequest request);
    string SanitizeInput(string input);
}

public class ValidationService : IValidationService
{
    private readonly string[] _validSpecies = { "Dog", "Cat", "Bird", "Rabbit", "Hamster", "Guinea Pig", "Fish", "Other" };
    private readonly string[] _validGenders = { "Male", "Female", "Neutered Male", "Spayed Female" };
    private readonly Regex _maliciousPatternRegex = new(@"(javascript:|vbscript:|onload|onerror|<script|</script)", RegexOptions.IgnoreCase);

    public ValidationResult ValidateCreatePetProfile(CreatePetProfileRequest request)
    {
        var errors = new List<string>();

        // Business logic validation beyond data annotations
        if (!string.IsNullOrEmpty(request.Species) && 
            !_validSpecies.Contains(request.Species, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add($"Species '{request.Species}' is not supported. Valid species: {string.Join(", ", _validSpecies)}");
        }

        if (!string.IsNullOrEmpty(request.Gender) && 
            !_validGenders.Contains(request.Gender, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add($"Gender '{request.Gender}' is not valid. Valid genders: {string.Join(", ", _validGenders)}");
        }

        // Check for malicious patterns
        if (ContainsMaliciousContent(request.Name))
            errors.Add("Pet name contains potentially dangerous content");

        if (!string.IsNullOrEmpty(request.Breed) && ContainsMaliciousContent(request.Breed))
            errors.Add("Breed contains potentially dangerous content");

        return new ValidationResult(errors.Count == 0, errors);
    }

    public ValidationResult ValidateUpdatePetProfile(UpdatePetProfileRequest request)
    {
        var errors = new List<string>();

        // Business logic validation
        if (!string.IsNullOrEmpty(request.Species) && 
            !_validSpecies.Contains(request.Species, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add($"Species '{request.Species}' is not supported. Valid species: {string.Join(", ", _validSpecies)}");
        }

        if (!string.IsNullOrEmpty(request.Gender) && 
            !_validGenders.Contains(request.Gender, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add($"Gender '{request.Gender}' is not valid. Valid genders: {string.Join(", ", _validGenders)}");
        }

        // Check for malicious patterns
        if (!string.IsNullOrEmpty(request.Name) && ContainsMaliciousContent(request.Name))
            errors.Add("Pet name contains potentially dangerous content");

        if (!string.IsNullOrEmpty(request.Breed) && ContainsMaliciousContent(request.Breed))
            errors.Add("Breed contains potentially dangerous content");

        return new ValidationResult(errors.Count == 0, errors);
    }

    public string SanitizeInput(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // HTML encode to prevent XSS
        var sanitized = HttpUtility.HtmlEncode(input);
        
        // Remove any remaining potentially dangerous patterns
        sanitized = _maliciousPatternRegex.Replace(sanitized, "");
        
        // Trim whitespace
        return sanitized.Trim();
    }

    private bool ContainsMaliciousContent(string input)
    {
        return !string.IsNullOrEmpty(input) && _maliciousPatternRegex.IsMatch(input);
    }
}

public record ValidationResult(bool IsValid, List<string> Errors);