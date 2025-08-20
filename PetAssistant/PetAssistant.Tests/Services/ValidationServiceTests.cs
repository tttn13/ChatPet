using FluentAssertions;
using PetAssistant.Models;
using PetAssistant.Services;
using Xunit;

namespace PetAssistant.Tests.Services;

public class ValidationServiceTests
{
    private readonly ValidationService _sut;

    public ValidationServiceTests()
    {
        _sut = new ValidationService();
    }

    #region ValidateCreatePetProfile Tests

    [Fact]
    public void ValidateCreatePetProfile_ValidInput_ReturnsValid()
    {
        // Arrange
        var request = new CreatePetProfileRequest
        {
            Name = "Buddy",
            Species = "Dog",
            Breed = "Golden Retriever",
            Age = 3,
            Gender = "Male"
        };

        // Act
        var result = _sut.ValidateCreatePetProfile(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Dog")]
    [InlineData("Cat")]
    [InlineData("Bird")]
    [InlineData("Rabbit")]
    [InlineData("Hamster")]
    [InlineData("Guinea Pig")]
    [InlineData("Fish")]
    [InlineData("Other")]
    [InlineData("dog")] // Test case insensitive
    [InlineData("DOG")]
    public void ValidateCreatePetProfile_ValidSpecies_ReturnsValid(string species)
    {
        // Arrange
        var request = new CreatePetProfileRequest
        {
            Name = "Test Pet",
            Species = species
        };

        // Act
        var result = _sut.ValidateCreatePetProfile(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Dinosaur")]
    [InlineData("Dragon")]
    [InlineData("Unicorn")]
    public void ValidateCreatePetProfile_InvalidSpecies_ReturnsErrors(string species)
    {
        // Arrange
        var request = new CreatePetProfileRequest
        {
            Name = "Test Pet",
            Species = species
        };

        // Act
        var result = _sut.ValidateCreatePetProfile(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().Contain("Species");
        result.Errors[0].Should().Contain("not supported");
    }

    [Theory]
    [InlineData("Male")]
    [InlineData("Female")]
    [InlineData("Neutered Male")]
    [InlineData("Spayed Female")]
    [InlineData("male")] // Test case insensitive
    [InlineData("FEMALE")]
    public void ValidateCreatePetProfile_ValidGender_ReturnsValid(string gender)
    {
        // Arrange
        var request = new CreatePetProfileRequest
        {
            Name = "Test Pet",
            Species = "Dog",
            Gender = gender
        };

        // Act
        var result = _sut.ValidateCreatePetProfile(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("Other")]
    [InlineData("N/A")]
    public void ValidateCreatePetProfile_InvalidGender_ReturnsErrors(string gender)
    {
        // Arrange
        var request = new CreatePetProfileRequest
        {
            Name = "Test Pet",
            Species = "Cat",
            Gender = gender
        };

        // Act
        var result = _sut.ValidateCreatePetProfile(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().Contain("Gender");
        result.Errors[0].Should().Contain("not valid");
    }

    [Theory]
    [InlineData("Fluffy<script>alert('xss')</script>")]
    [InlineData("Test javascript:alert(1)")]
    [InlineData("Pet onerror=alert(1)")]
    [InlineData("<script>evil</script>")]
    public void ValidateCreatePetProfile_MaliciousName_ReturnsErrors(string maliciousName)
    {
        // Arrange
        var request = new CreatePetProfileRequest
        {
            Name = maliciousName,
            Species = "Dog"
        };

        // Act
        var result = _sut.ValidateCreatePetProfile(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().Contain("Pet name contains potentially dangerous content");
    }

    [Theory]
    [InlineData("Golden<script>alert('xss')</script>Retriever")]
    [InlineData("javascript:void(0)")]
    [InlineData("vbscript:msgbox")]
    public void ValidateCreatePetProfile_MaliciousBreed_ReturnsErrors(string maliciousBreed)
    {
        // Arrange
        var request = new CreatePetProfileRequest
        {
            Name = "Safe Name",
            Species = "Dog",
            Breed = maliciousBreed
        };

        // Act
        var result = _sut.ValidateCreatePetProfile(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().Contain("Breed contains potentially dangerous content");
    }

    [Fact]
    public void ValidateCreatePetProfile_MultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var request = new CreatePetProfileRequest
        {
            Name = "<script>alert('xss')</script>",
            Species = "Dinosaur",
            Gender = "Unknown"
        };

        // Act
        var result = _sut.ValidateCreatePetProfile(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
        result.Errors.Should().Contain(e => e.Contains("Species"));
        result.Errors.Should().Contain(e => e.Contains("Gender"));
        result.Errors.Should().Contain(e => e.Contains("Pet name"));
    }

    #endregion

    #region ValidateUpdatePetProfile Tests

    [Fact]
    public void ValidateUpdatePetProfile_ValidPartialUpdate_ReturnsValid()
    {
        // Arrange
        var request = new UpdatePetProfileRequest
        {
            Name = "New Name",
            Age = 5
        };

        // Act
        var result = _sut.ValidateUpdatePetProfile(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateUpdatePetProfile_EmptyRequest_ReturnsValid()
    {
        // Arrange
        var request = new UpdatePetProfileRequest();

        // Act
        var result = _sut.ValidateUpdatePetProfile(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateUpdatePetProfile_InvalidSpecies_ReturnsErrors()
    {
        // Arrange
        var request = new UpdatePetProfileRequest
        {
            Species = "Alien"
        };

        // Act
        var result = _sut.ValidateUpdatePetProfile(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().Contain("Species");
    }

    [Theory]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("javascript:alert(1)")]
    public void ValidateUpdatePetProfile_MaliciousName_ReturnsErrors(string maliciousName)
    {
        // Arrange
        var request = new UpdatePetProfileRequest
        {
            Name = maliciousName
        };

        // Act
        var result = _sut.ValidateUpdatePetProfile(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().Contain("Pet name contains potentially dangerous content");
    }

    #endregion

    #region SanitizeInput Tests

    [Theory]
    [InlineData("<script>alert('xss')</script>", "&lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt;")]
    [InlineData("Normal text", "Normal text")]
    [InlineData("Text with <b>HTML</b>", "Text with &lt;b&gt;HTML&lt;/b&gt;")]
    public void SanitizeInput_RemovesHtmlTags(string input, string expected)
    {
        // Act
        var result = _sut.SanitizeInput(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("javascript:alert('xss')", "alert(&#39;xss&#39;)")]
    [InlineData("vbscript:msgbox", "msgbox")]
    public void SanitizeInput_RemovesMaliciousPatterns(string input, string expected)
    {
        // Act
        var result = _sut.SanitizeInput(input);

        // Assert
        result.Should().Be(expected);
        result.Should().NotContain("javascript:");
        result.Should().NotContain("vbscript:");
        result.Should().NotContain("onload");
        result.Should().NotContain("onerror");
    }

    [Theory]
    [InlineData("  text  ", "text")]
    [InlineData("\t\ttext\t\t", "text")]
    [InlineData("\ntext\n", "text")]
    [InlineData("  multiple  spaces  ", "multiple  spaces")]
    public void SanitizeInput_TrimsWhitespace(string input, string expected)
    {
        // Act
        var result = _sut.SanitizeInput(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void SanitizeInput_NullOrEmpty_ReturnsInput(string input)
    {
        // Act
        var result = _sut.SanitizeInput(input);

        // Assert
        result.Should().Be(input);
    }

    [Fact]
    public void SanitizeInput_ComplexMaliciousInput_SanitizesCompletely()
    {
        // Arrange
        var input = "  <script>javascript:alert('xss')</script>Normal Text<img onerror=alert(1) src=x>  ";

        // Act
        var result = _sut.SanitizeInput(input);

        // Assert
        result.Should().NotContain("<script>");
        result.Should().NotContain("</script>");
        result.Should().NotContain("javascript:");
        result.Should().NotContain("onerror");
        result.Should().Contain("Normal Text");
        result.Should().NotStartWith(" ");
        result.Should().NotEndWith(" ");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ValidateCreatePetProfile_NullGender_ReturnsValid()
    {
        // Arrange
        var request = new CreatePetProfileRequest
        {
            Name = "Test Pet",
            Species = "Cat",
            Gender = null
        };

        // Act
        var result = _sut.ValidateCreatePetProfile(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateCreatePetProfile_NullBreed_ReturnsValid()
    {
        // Arrange
        var request = new CreatePetProfileRequest
        {
            Name = "Test Pet",
            Species = "Cat",
            Breed = null
        };

        // Act
        var result = _sut.ValidateCreatePetProfile(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateCreatePetProfile_EmptyStringBreed_ReturnsValid()
    {
        // Arrange
        var request = new CreatePetProfileRequest
        {
            Name = "Test Pet",
            Species = "Cat",
            Breed = ""
        };

        // Act
        var result = _sut.ValidateCreatePetProfile(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Cat", "")]
    [InlineData("Fish", null)]
    public void ValidateCreatePetProfile_SpeciesWithWhitespaceOrNull_HandlesCorrectly(string species, string? emptyValue)
    {
        // Arrange
        var request = new CreatePetProfileRequest
        {
            Name = "Test Pet",
            Species = species,
            Gender = emptyValue
        };

        // Act
        var result = _sut.ValidateCreatePetProfile(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion
}