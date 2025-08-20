# Static vs Instance Methods in Test Helper Functions

## The Question
When creating helper methods for test classes, should they be `static` or instance methods?

## Static Helper Methods (Recommended for Pure Functions)

```csharp
private static Mock<IValidationService> CreateValidationServiceMock(string? sanitizedOutput = null)
{
    var mock = new Mock<IValidationService>();
    mock.Setup(x => x.SanitizeInput(It.IsAny<string>()))
        .Returns<string>(input => sanitizedOutput ?? input);
    return mock;
}
```

## Why Static Makes Sense Here

### 1. **No Instance Dependencies**
The method doesn't use `_factory` or any other instance fields from the test class.

### 2. **Pure Function**
Given the same inputs, always returns the same type of mock object. No side effects or state modifications.

### 3. **No Side Effects**
Doesn't modify any instance state of the test class.

### 4. **Utility Function**
Just creates and configures a mock object - essentially a factory function.

## Alternative: Non-Static Approach

```csharp
private Mock<IValidationService> CreateValidationServiceMock(string? sanitizedOutput = null)
{
    // Still doesn't use 'this' or any instance members
    var mock = new Mock<IValidationService>();
    mock.Setup(x => x.SanitizeInput(It.IsAny<string>()))
        .Returns<string>(input => sanitizedOutput ?? input);
    return mock;
}
```

**Problem**: This approach provides no benefits since the method doesn't access instance state.

## Benefits of Static Helper Methods

| Benefit | Description |
|---------|-------------|
| **Performance** | Slightly faster execution (no `this` reference needed) |
| **Clear Intent** | Shows these are pure utility functions with no dependencies |
| **Reusability** | Could be moved to a static utility class later if needed |
| **Thread Safety** | No instance state to worry about in concurrent scenarios |
| **Compiler Optimization** | Compiler can optimize static calls better |

## When to Use Non-Static Helper Methods

Use instance (non-static) methods when the helper needs access to test class state:

```csharp
public class MyTests
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly TestConfiguration _config;

    private Mock<IValidationService> CreateValidationServiceMock()
    {
        // Accessing instance fields - NEEDS to be non-static
        var settings = _factory.Services.GetService<IConfiguration>();
        var testMode = _config.IsDebugMode;
        
        var mock = new Mock<IValidationService>();
        // Configure based on instance state...
        return mock;
    }
}
```

## Decision Tree

```
Does the helper method need access to instance fields?
├─ YES → Use instance method (non-static)
└─ NO → Use static method
    ├─ Pure function with no side effects? → ✅ Static
    ├─ Creates objects without dependencies? → ✅ Static
    └─ Just utility/factory function? → ✅ Static
```

## Real-World Examples

### ✅ Good Candidates for Static

```csharp
// Mock factories - no dependencies
private static Mock<ITelemetryService> CreateTelemetryServiceMock()
private static Mock<IGroqService> CreateGroqServiceMock(string response)

// Data builders - no dependencies  
private static CreatePetProfileRequest BuildValidRequest()
private static PetProfile BuildTestProfile(string name)

// Utility functions - no dependencies
private static void RemoveService<T>(IServiceCollection services)
private static string GenerateRandomSessionId()
```

### ❌ Should Be Instance Methods

```csharp
// Accesses _factory instance field
private HttpClient CreateAuthenticatedClient()
{
    return _factory.WithWebHostBuilder(builder => /* ... */).CreateClient();
}

// Accesses test configuration
private Mock<IService> CreateServiceMockForCurrentTest()
{
    var config = _testConfig.GetCurrentTestSettings();
    // ...
}
```

## Best Practices

1. **Start with static** - Default to static unless you need instance access
2. **Keep helpers pure** - Avoid side effects in helper methods
3. **Group related helpers** - Put similar static helpers together
4. **Consider extraction** - If you have many static helpers, consider moving them to a separate utility class
5. **Be consistent** - Follow the same pattern across your test suite

## Conclusion

For mock factory methods and pure utility functions in tests, **static is the right choice** because:

- ✅ They don't need instance state
- ✅ They're pure functions
- ✅ Better performance and clarity
- ✅ Easier to reason about and test

Use instance methods only when you actually need access to test class fields or properties.