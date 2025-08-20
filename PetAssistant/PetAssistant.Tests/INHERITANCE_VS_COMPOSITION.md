# Inheritance vs Composition in Test Design

## Overview
When designing test infrastructure, you can choose between inheritance (base classes) or composition (helper classes). This document compares both approaches and explains why composition is often the better choice.

## Inheritance (Base Class) Approach

```csharp
// INHERITANCE - "IS-A" relationship
public abstract class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>
{
    protected readonly WebApplicationFactory<Program> Factory;
    
    protected HttpClient CreateTestClient(...) 
    { 
        // Shared test setup logic
    }
}

public class ChatEndpointsIntegrationTests : IntegrationTestBase
{
    public ChatEndpointsIntegrationTests(WebApplicationFactory<Program> factory) 
        : base(factory) // Must call base constructor
    { 
    }
    
    [Fact] 
    public async Task Test() 
    { 
        var client = CreateTestClient(...); // Inherited method
    }
}
```

## Composition (Helper Class) Approach

```csharp
// COMPOSITION - "HAS-A" relationship
public class IntegrationTestHelper
{
    private readonly WebApplicationFactory<Program> _factory;
    
    public HttpClient CreateTestClient(...) 
    { 
        // Shared test setup logic
    }
}

public class ChatEndpointsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly IntegrationTestHelper _helper; // Composition
    
    public ChatEndpointsIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _helper = new IntegrationTestHelper(factory); // Inject dependency
    }
    
    [Fact] 
    public async Task Test() 
    { 
        var client = _helper.CreateTestClient(...); // Delegate to helper
    }
}
```

## Key Differences

| Aspect | Inheritance (Base Class) | Composition (Helper) |
|--------|-------------------------|---------------------|
| **Relationship** | IS-A (test class IS a base test) | HAS-A (test class HAS a helper) |
| **Coupling** | Tight - locked into inheritance hierarchy | Loose - can swap helpers easily |
| **Flexibility** | Can't inherit from multiple bases | Can use multiple helpers |
| **Testing** | Hard to test base class methods | Easy to unit test helper |
| **Reusability** | Only via inheritance | Can be used anywhere |
| **Dependency** | Constructor must call base | Simple dependency injection |
| **Mockability** | Cannot mock base class | Can mock helper interface |
| **Evolution** | Changing base affects all children | Helper changes are isolated |

## Why Composition is More Testable

### 1. **Helper Classes Can Be Unit Tested**
```csharp
[Fact]
public void TestHelper_CreateTestClient_ConfiguresServicesProperly()
{
    // Arrange
    var factory = new Mock<WebApplicationFactory<Program>>();
    var helper = new IntegrationTestHelper(factory.Object);
    
    // Act & Assert
    // You can test the helper's logic independently
}
```
With inheritance, you can't easily test the base class methods in isolation.

### 2. **Helpers Can Be Mocked**
```csharp
public interface IIntegrationTestHelper
{
    HttpClient CreateTestClient(...);
}

[Fact]
public void SomeTest()
{
    // You can mock the helper for testing
    var mockHelper = new Mock<IIntegrationTestHelper>();
    mockHelper.Setup(x => x.CreateTestClient()).Returns(mockClient);
    
    // Cannot mock a base class this way!
}
```

### 3. **Multiple Helpers Can Be Composed**
```csharp
public class MyTest
{
    private readonly DatabaseHelper _dbHelper;
    private readonly ApiHelper _apiHelper;
    private readonly AuthHelper _authHelper;
    
    // Can combine multiple helpers
    // With inheritance, you're limited to one base class
}
```

### 4. **Helpers Are Explicit Dependencies**
```csharp
// Clear what dependencies the test has
public MyTest(DatabaseHelper dbHelper, ApiHelper apiHelper)
{
    _dbHelper = dbHelper;
    _apiHelper = apiHelper;
}

// vs hidden base class dependencies
public MyTest() : base() // What does base need? Not clear!
{
}
```

### 5. **Helpers Enable Dependency Injection**
```csharp
// Can register helpers in DI container
services.AddScoped<IntegrationTestHelper>();
services.AddScoped<DatabaseTestHelper>();

// Test classes can then receive them via constructor injection
public class MyTest
{
    public MyTest(IntegrationTestHelper helper) // DI works!
    {
        _helper = helper;
    }
}
```

### 6. **Helpers Are More Focused (Single Responsibility)**
```csharp
// Each helper has one job
public class MockServiceHelper { /* only mocking */ }
public class DatabaseSeeder { /* only seeding */ }
public class ApiClientHelper { /* only API setup */ }

// vs a base class trying to do everything
public abstract class TestBase 
{
    // Mocking, seeding, API setup, logging, etc.
    // Violates Single Responsibility Principle
}
```

## Real-World Example: Testing the Helper

With composition, you can write tests for your test infrastructure:

```csharp
public class IntegrationTestHelperTests
{
    [Fact]
    public void CreateTestClient_RemovesAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IGroqService, GroqService>();
        
        // Act
        IntegrationTestHelper.RemoveAllApplicationServices(services);
        
        // Assert
        services.Should().NotContain(s => s.ServiceType == typeof(IGroqService));
    }
    
    [Fact]
    public void CreateGroqServiceMock_ReturnsConfiguredMock()
    {
        // Act
        var mock = IntegrationTestHelper.CreateGroqServiceMock("test", null, "session");
        
        // Assert
        var result = await mock.Object.GetPetAdviceAsync("query", null, null);
        result.response.Should().Be("test");
        result.sessionId.Should().Be("session");
    }
}
```

This level of testing is much harder (or impossible) with inheritance-based approaches.

## When to Use Each Approach

### Use Inheritance When:
- You have a true IS-A relationship
- All derived classes need the exact same behavior
- The base functionality is unlikely to change
- You don't need to test the base class independently

### Use Composition When:
- You need flexibility to combine behaviors
- You want to test your test infrastructure
- Different test classes need different combinations of helpers
- You value loose coupling and maintainability
- You follow SOLID principles

## Conclusion

**Composition wins for test infrastructure** because:
1. ✅ More testable - helpers can be unit tested
2. ✅ More flexible - combine multiple helpers
3. ✅ More maintainable - changes are isolated
4. ✅ More explicit - dependencies are clear
5. ✅ Follows SOLID principles - especially Single Responsibility
6. ✅ Enables mocking and dependency injection

The slight extra verbosity of composition (`_helper.Method()` vs `Method()`) is a small price to pay for these significant benefits in testability and maintainability.