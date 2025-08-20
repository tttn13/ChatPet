# Factory Pattern in .NET Framework

## What "Factory" Means in .NET

A **Factory** in .NET is a class/method that **creates and configures objects** for you, hiding the complexity of instantiation. The term comes from the Factory Design Pattern and is used consistently across the .NET framework.

## Simple Definition

A **Factory** is a class or method that:
1. **Creates objects** for you
2. **Applies sensible default configuration** 
3. **Hides the complexity** of setup

**Factory = Object Creator + Default Configuration**

## Real-World Analogy
Think of a car factory:
- You don't assemble the car yourself (complex creation)
- You get a fully functional car (default configuration)
- It has standard features: engine, wheels, brakes (sensible defaults)
- You might choose color/model (some customization)

## Without vs With Factory

### ❌ Without Factory (Manual Creation)
```csharp
// You have to know and configure everything yourself
var connection = new SqlConnection(connectionString);
connection.Open();
var command = new SqlCommand(sql, connection);
command.CommandTimeout = 30;
command.CommandType = CommandType.Text;
var adapter = new SqlDataAdapter(command);
var dataSet = new DataSet();
adapter.Fill(dataSet);
// ... lots of manual setup
```

### ✅ With Factory (Simple + Configured)
```csharp
// Factory handles all the complexity and provides good defaults
var dataAccess = DatabaseFactory.CreateSqlDataAccess(connectionString);
var data = dataAccess.ExecuteQuery(sql); // Just works!
```

## Key Factory Characteristics

| Aspect | What Factory Provides |
|--------|----------------------|
| **Object Creation** | `new SomeComplexObject()` |
| **Default Configuration** | Standard settings that work in most cases |
| **Simplicity** | One method call vs many lines of setup |
| **Consistency** | Same configuration every time |
| **Customization** | Optional parameters for when you need something different |

## Factory Examples in Action

### HttpClientFactory
```csharp
// ❌ Manual (problems: socket exhaustion, no configuration)
var client = new HttpClient();

// ✅ Factory (good defaults: connection pooling, timeout, retry policies)
var client = httpClientFactory.CreateClient("apiClient");
```

### TestMockFactory
```csharp
// ❌ Manual mock creation (repetitive, error-prone)
var mock = new Mock<IValidationService>();
mock.Setup(x => x.SanitizeInput(It.IsAny<string>()))
    .Returns<string>(input => input ?? "");
mock.Setup(x => x.ValidateCreatePetProfile(It.IsAny<CreatePetProfileRequest>()))
    .Returns(new ValidationResult(true, new List<string>()));
// ... more setup

// ✅ Factory approach (simple + good defaults)
var mock = TestMockFactory.CreateValidationService(); // Ready to use!
```

### WebApplicationFactory
```csharp
// ❌ Manual (impossible - too complex)
var server = new TestServer(/* hundreds of lines of setup */);

// ✅ Factory (good defaults: full ASP.NET pipeline, DI, middleware)
var client = webAppFactory.CreateClient();
```

## The Factory Philosophy

**Factory = "I want an object that just works, with good defaults, without me having to know all the details"**

It's like ordering a "standard combo meal" instead of selecting each ingredient individually - you get a complete, working solution with sensible defaults, and you can customize if needed.

## Common .NET Factories and What They Create

### 1. **WebApplicationFactory<Program>**
```csharp
public class MyTests : IClassFixture<WebApplicationFactory<Program>>
{
    // WebApplicationFactory creates a complete ASP.NET Core application for testing
}
```

**What it creates**: A fully configured test web application including:
- Service container setup
- Middleware pipeline configuration  
- Test server creation
- HTTP client creation
- Environment configuration

### 2. **HttpClientFactory**
```csharp
// Instead of manually managing HttpClient lifecycle:
using var client = new HttpClient(); // BAD - causes socket exhaustion

// Use factory for proper lifecycle management:
var client = httpClientFactory.CreateClient("apiClient"); // GOOD
```

**What it creates**: Properly configured HttpClient instances with:
- Connection pooling
- Lifecycle management
- Named client configurations
- Retry policies and middleware

### 3. **LoggerFactory**
```csharp
var logger = loggerFactory.CreateLogger<MyService>();
var logger2 = loggerFactory.CreateLogger("CustomCategory");
```

**What it creates**: Configured logger instances with:
- Proper log level filtering
- Category-based logging
- Multiple provider support (Console, File, etc.)
- Structured logging capabilities

### 4. **ServiceProviderFactory**
```csharp
var services = new ServiceCollection();
var factory = new DefaultServiceProviderFactory();
var provider = factory.CreateServiceProvider(services);
```

**What it creates**: The dependency injection container with:
- Service registrations
- Lifetime management (Singleton, Scoped, Transient)
- Dependency resolution
- Disposal tracking

### 5. **ChannelFactory<T>** (WCF)
```csharp
var factory = new ChannelFactory<IMyService>(binding, endpoint);
var client = factory.CreateChannel();
```

**What it creates**: WCF service client proxies with:
- Binding configuration
- Endpoint setup
- Security settings
- Communication channel management

### 6. **TaskFactory**
```csharp
public class Task
{
    public static Task Run(Action action) // Factory method
    public static Task<T> FromResult<T>(T result) // Factory method
}

var task = Task.Run(() => DoWork());
```

**What it creates**: Configured Task instances with:
- Proper thread scheduling
- Task scheduler assignment
- Cancellation token support
- Exception handling setup

### 7. **ConnectionFactory** (Entity Framework)
```csharp
public class DbContextFactory : IDbContextFactory<MyDbContext>
{
    public MyDbContext CreateDbContext()
    {
        return new MyDbContext(options);
    }
}
```

**What it creates**: Database context instances with:
- Connection string configuration
- Provider-specific setup
- Migration tracking
- Change tracking configuration

### 8. **HostBuilderFactory**
```csharp
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services => { })
    .Build();
```

**What it creates**: Application host instances with:
- Configuration system setup
- Logging infrastructure
- Service container initialization
- Application lifetime management

### 9. **JsonSerializerFactory**
```csharp
var options = new JsonSerializerOptions();
var serializer = JsonSerializer.Create(options);
```

**What it creates**: JSON serializer instances with:
- Serialization options
- Custom converters
- Naming policies
- Performance optimizations

### 10. **TestMockFactory** (Our Implementation)
```csharp
public static class TestMockFactory
{
    public static Mock<IValidationService> CreateValidationService(string? sanitizedOutput = null)
    {
        var mock = new Mock<IValidationService>();
        mock.Setup(x => x.SanitizeInput(It.IsAny<string>()))
            .Returns<string>(input => sanitizedOutput ?? input);
        return mock;
    }
}
```

**What it creates**: Pre-configured mock objects with:
- Standard test behavior setup
- Consistent mock configurations
- Reusable test doubles
- Simplified test arrangement

## Factory Creation Patterns

### 1. **Static Factory Methods**
```csharp
// Creates objects through static methods
var task = Task.Run(() => DoWork());
var logger = LoggerFactory.Create(builder => builder.AddConsole());
```

### 2. **Factory Classes**
```csharp
// Dedicated classes for object creation
var factory = new WebApplicationFactory<Program>();
var app = factory.CreateClient();
```

### 3. **Generic Factories**
```csharp
// Type-safe object creation
public interface IFactory<T>
{
    T Create();
}
```

### 4. **Factory Interfaces**
```csharp
// Abstracted factory contracts
public interface IDbContextFactory<TContext>
{
    TContext CreateDbContext();
}
```

## Why .NET Uses Factory Pattern Extensively

| Benefit | What It Provides |
|---------|------------------|
| **Encapsulates Complexity** | Hides intricate object setup and configuration |
| **Manages Lifecycles** | Handles object creation, disposal, and resource management |
| **Provides Consistency** | Ensures objects are created with standard configurations |
| **Enables Configuration** | Allows customization through parameters and options |
| **Promotes Reusability** | Standardizes object creation across applications |
| **Supports Testing** | Enables easy mocking and test object creation |

## Factory vs Related Patterns

### Factory vs Builder
```csharp
// Factory: Creates objects directly
var app = WebApplicationFactory.Create();

// Builder: Constructs objects step-by-step  
var app = WebApplication.CreateBuilder()
    .AddServices()
    .AddMiddleware()
    .Build();
```

### Factory vs Provider
```csharp
// Factory: Creates new objects
var logger = LoggerFactory.CreateLogger<Service>();

// Provider: Gives access to existing objects
var service = ServiceProvider.GetService<IMyService>();
```

### Factory vs Repository
```csharp
// Factory: Creates objects
var context = DbContextFactory.CreateDbContext();

// Repository: Manages data access
var users = UserRepository.GetAll();
```

## Conclusion

In .NET, **"Factory" consistently means "object creator"** across the entire framework. This naming convention provides:

- ✅ **Clear Intent** - Immediately communicates purpose
- ✅ **Familiar Pattern** - Developers understand expectations
- ✅ **Consistent API** - Similar usage patterns across different factories
- ✅ **Predictable Behavior** - Standard lifecycle and configuration management

Whether it's `WebApplicationFactory` creating test applications or `TestMockFactory` creating mock objects, the pattern remains the same: **encapsulating complex object creation behind a simple, consistent interface**.