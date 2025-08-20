# IDisposable and Using Statement Guide

## When to Implement IDisposable

### Objects That Need Cleanup

#### 1. Unmanaged Resources
- **File handles**: `FileStream`, `StreamWriter`, `StreamReader`
- **Network connections**: `HttpClient`, `TcpClient`, `Socket`, `WebSocket`
- **Database connections**: `SqlConnection`, `DbContext`, `NpgsqlConnection`
- **OS handles**: `Process`, `Mutex`, `Semaphore`, `EventWaitHandle`
- **Graphics resources**: `Bitmap`, `Graphics`, `Brush`, `Font`, `Image`
- **Memory-mapped files**: `MemoryMappedFile`, `MemoryMappedViewStream`

#### 2. Objects That Own IDisposable Fields
```csharp
public class MyService : IDisposable
{
    private readonly HttpClient _client = new();  // IDisposable
    private readonly FileStream _file;             // IDisposable
    private bool _disposed = false;
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
                _client?.Dispose();
                _file?.Dispose();
            }
            _disposed = true;
        }
    }
}
```

#### 3. Event Subscriptions (Memory Leak Prevention)
```csharp
public class EventListener : IDisposable
{
    private readonly EventSource _source;
    
    public EventListener(EventSource source)
    {
        _source = source;
        _source.SomeEvent += OnEvent;  // Subscribe
    }
    
    public void Dispose()
    {
        _source.SomeEvent -= OnEvent;  // Unsubscribe to prevent leak
    }
    
    private void OnEvent(object sender, EventArgs e) { }
}
```

## How to Identify If Something Needs IDisposable

### Type Indicators
- Implements `IDisposable` or `IAsyncDisposable`
- Uses `extern`, `DllImport`, or P/Invoke
- Holds references to OS resources
- Maintains long-lived subscriptions
- Uses `unsafe` code with allocated memory
- Creates threads, timers, or tasks

### IDE/Compiler Hints
- **Warning CA2000**: "Dispose objects before losing scope"
- **Warning CA2213**: "Disposable fields should be disposed"
- IntelliSense shows `IDisposable` in inheritance chain
- `using` statement compatibility check

### Common Patterns Requiring Disposal
```csharp
// File I/O
var file = File.Open("data.txt", FileMode.Open);
var writer = new StreamWriter("output.txt");
var reader = new StreamReader("input.txt");

// Network Operations
var client = new HttpClient();
var tcpClient = new TcpClient();
var response = await client.GetAsync("https://api.example.com");

// Database Operations
var connection = new SqlConnection(connectionString);
var command = new SqlCommand(query, connection);
var context = new MyDbContext();

// Threading & Synchronization
var timer = new Timer(callback);
var cts = new CancellationTokenSource();
var semaphore = new SemaphoreSlim(1);
var mutex = new Mutex();

// Graphics & UI
var bitmap = new Bitmap(100, 100);
var graphics = Graphics.FromImage(bitmap);
var font = new Font("Arial", 12);
```

## The Using Statement

### Basic Syntax

#### Traditional Using Block
```csharp
using (var stream = File.OpenRead("file.txt"))
{
    // Use stream here
    // stream.Dispose() is automatically called at the end
} // Dispose called here, even if exception occurs
```

#### Using Declaration (C# 8.0+)
```csharp
using var stream = File.OpenRead("file.txt");
// Use stream for rest of the scope
// Dispose called when stream goes out of scope
```

### How Using Works

The `using` statement is syntactic sugar that ensures `Dispose()` is called. It's equivalent to:

```csharp
// This using statement:
using (var resource = new SomeDisposableResource())
{
    resource.DoSomething();
}

// Is compiled to approximately:
SomeDisposableResource resource = null;
try
{
    resource = new SomeDisposableResource();
    resource.DoSomething();
}
finally
{
    if (resource != null)
    {
        resource.Dispose();
    }
}
```

### Multiple Resources

#### Nested Using Statements
```csharp
using (var connection = new SqlConnection(connectionString))
{
    using (var command = new SqlCommand(query, connection))
    {
        // Use both resources
    }
}
```

#### Multiple Using Declarations
```csharp
using (var connection = new SqlConnection(connectionString))
using (var command = new SqlCommand(query, connection))
{
    // Use both resources
}
```

#### Modern Using Declarations (C# 8.0+)
```csharp
using var connection = new SqlConnection(connectionString);
using var command = new SqlCommand(query, connection);
// Both disposed at end of scope in reverse order
```

### Async Disposal (C# 8.0+)

For asynchronous cleanup, implement `IAsyncDisposable`:

```csharp
public class AsyncResource : IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        await CleanupAsync();
    }
}

// Usage
await using var resource = new AsyncResource();
// DisposeAsync() called at end of scope

// Or with block syntax
await using (var resource = new AsyncResource())
{
    // Use resource
}
```

### Using Statement Best Practices

#### 1. Always Use Using for IDisposable Objects
```csharp
// ❌ Bad - Manual disposal, may not happen if exception occurs
var file = File.OpenRead("data.txt");
ProcessFile(file);
file.Dispose(); // Might not be reached

// ✅ Good - Guaranteed disposal
using var file = File.OpenRead("data.txt");
ProcessFile(file);
```

#### 2. Don't Dispose Objects You Don't Own
```csharp
// ❌ Bad - Disposing shared instance
public void ProcessData(HttpClient client)
{
    using (client) // Don't dispose parameters!
    {
        // Process
    }
}

// ✅ Good - Let owner handle disposal
public void ProcessData(HttpClient client)
{
    // Use client without disposing
}
```

#### 3. Be Careful with Factory Methods
```csharp
// ❌ Potentially bad - Check if GetStream() returns new instance
using var stream = GetStream();

// ✅ Good - Clear ownership
using var stream = CreateNewStream(); // Name implies new instance
```

#### 4. Use Using Declarations for Cleaner Code
```csharp
// Old style - more nesting
public void ProcessFile(string path)
{
    using (var file = File.OpenRead(path))
    {
        using (var reader = new StreamReader(file))
        {
            // Process
        }
    }
}

// Modern style - less nesting
public void ProcessFile(string path)
{
    using var file = File.OpenRead(path);
    using var reader = new StreamReader(file);
    // Process
} // Both disposed here
```

### Common Pitfalls

#### 1. Disposing Too Early
```csharp
// ❌ Bad
Task<string> ReadAsync()
{
    using var client = new HttpClient();
    return client.GetStringAsync("https://example.com"); // Client disposed before task completes!
}

// ✅ Good
async Task<string> ReadAsync()
{
    using var client = new HttpClient();
    return await client.GetStringAsync("https://example.com");
}
```

#### 2. Not Disposing in Exception Cases
```csharp
// ❌ Bad - Leak if exception occurs
FileStream file = null;
try
{
    file = File.OpenRead("data.txt");
    ProcessFile(file);
}
catch (Exception ex)
{
    // Handle error
}
file?.Dispose(); // Might not be reached

// ✅ Good - Using ensures disposal
using var file = File.OpenRead("data.txt");
ProcessFile(file); // Disposed even if exception occurs
```

#### 3. Returning Disposed Objects
```csharp
// ❌ Bad
public Stream GetData()
{
    using var stream = new MemoryStream();
    // Fill stream
    return stream; // Returned stream is already disposed!
}

// ✅ Good
public Stream GetData()
{
    var stream = new MemoryStream();
    // Fill stream
    return stream; // Caller is responsible for disposal
}
```

## Disposal Pattern Implementation

### Basic Pattern
```csharp
public class ResourceHolder : IDisposable
{
    private bool _disposed = false;
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
            }
            
            // Free unmanaged resources
            
            _disposed = true;
        }
    }
}
```

### With Finalizer (for Unmanaged Resources)
```csharp
public class UnmanagedResourceHolder : IDisposable
{
    private IntPtr _unmanagedHandle;
    private bool _disposed = false;
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
            }
            
            // Free unmanaged resources
            if (_unmanagedHandle != IntPtr.Zero)
            {
                NativeMethods.ReleaseHandle(_unmanagedHandle);
                _unmanagedHandle = IntPtr.Zero;
            }
            
            _disposed = true;
        }
    }
    
    ~UnmanagedResourceHolder()
    {
        Dispose(false);
    }
}
```

## Real-World Example: TelemetryService

### Why TelemetryService Implements IDisposable

The `TelemetryService` implements `IDisposable` specifically because it creates a `Meter` object:

```csharp
public class TelemetryService : ITelemetryService, IDisposable
{
    private readonly Meter _meter;
    
    public TelemetryService(ILogger<TelemetryService> logger)
    {
        _meter = new Meter("PetAssistant.API", "1.0.0");
        // Creates counters and histograms using _meter...
    }
    
    public void Dispose()
    {
        _meter.Dispose();
    }
}
```

### What Meter Does

`Meter` (from `System.Diagnostics.Metrics`) is part of .NET's OpenTelemetry infrastructure. It:
- Registers itself with the global metrics collection system
- Holds references to all instruments (counters, histograms) created from it
- May connect to external telemetry collectors (Prometheus, Application Insights, etc.)
- Maintains internal buffers for metric aggregation
- Keeps callbacks and listeners alive

### Why Meter Needs Cleanup

Without disposing the `Meter`, several problems occur:

1. **Memory Leak** - The meter stays registered in the global metrics system, keeping all its instruments and the service itself in memory
2. **Resource Leak** - Any connections to telemetry collectors remain open
3. **Continued Metric Collection** - The meter might continue collecting metrics even after the service should be "dead"
4. **Prevents Garbage Collection** - The global registry holds a reference, so the entire `TelemetryService` can't be GC'd

### In Practice

When ASP.NET Core shuts down or when the service is replaced (e.g., during testing), the DI container calls `Dispose()` on all registered IDisposable services. This ensures:
- The meter unregisters from the global system
- All metric instruments are cleaned up
- Memory is properly released
- No "zombie" metrics continue being collected

Without `IDisposable`, you'd have orphaned meters accumulating in memory every time the service is recreated, eventually causing performance issues or crashes in long-running applications.

### Lesson Learned

Even if a class doesn't directly handle files or network connections, it still needs `IDisposable` if it:
- Uses types that register with global/static systems
- Creates objects that live beyond the normal request/response cycle
- Holds references that prevent garbage collection

This is why checking if your dependencies implement `IDisposable` is crucial - if they do, your class likely needs to implement it too.

## Critical Concept: Ownership vs. Usage

### The Golden Rule: "You Dispose What You Create"

Not all IDisposable fields require your class to implement IDisposable. The key distinction is **ownership**.

### Example: GroqService vs TelemetryService

#### GroqService - Doesn't Need IDisposable
```csharp
public class GroqService : IGroqService // No IDisposable
{
    private readonly HttpClient _httpClient; // IDisposable field
    
    public GroqService(HttpClient httpClient, ...) // Injected
    {
        _httpClient = httpClient; // Just storing reference, not owning
    }
    // No Dispose method needed!
}
```

#### TelemetryService - Needs IDisposable
```csharp
public class TelemetryService : ITelemetryService, IDisposable
{
    private readonly Meter _meter; // IDisposable field
    
    public TelemetryService(ILogger<TelemetryService> logger)
    {
        _meter = new Meter("PetAssistant.API", "1.0.0"); // Creating = owning
    }
    
    public void Dispose()
    {
        _meter.Dispose(); // Must dispose what we created
    }
}
```

### The Key Difference: Ownership

**GroqService:**
- `HttpClient` is **injected** through constructor
- GroqService doesn't create it, doesn't own it
- ASP.NET Core's DI container manages its lifetime
- The DI container will dispose it when appropriate
- **Result:** No IDisposable needed

**TelemetryService:**
- `Meter` is **created** inside the constructor
- TelemetryService owns the Meter
- TelemetryService is responsible for cleanup
- **Result:** Must implement IDisposable

### Practical Examples

#### ❌ Wrong - Disposing Injected Dependencies
```csharp
public class BadService : IDisposable
{
    private readonly HttpClient _client;
    
    public BadService(HttpClient client) // Injected
    {
        _client = client;
    }
    
    public void Dispose()
    {
        _client.Dispose(); // ❌ Don't dispose what you don't own!
        // This could break other services using the same client
    }
}
```

#### ✅ Correct - Only Dispose What You Create
```csharp
public class GoodService : IDisposable
{
    private readonly HttpClient _injectedClient;
    private readonly FileStream _myFile;
    
    public GoodService(HttpClient client) // Injected
    {
        _injectedClient = client; // Don't dispose this
        _myFile = File.OpenRead("data.txt"); // We created this
    }
    
    public void Dispose()
    {
        // Only dispose what we created
        _myFile?.Dispose(); // ✅ We own this
        // Don't touch _injectedClient
    }
}
```

### HttpClient Special Considerations

In modern ASP.NET Core, `HttpClient` is typically registered with DI using:

```csharp
// In Program.cs or Startup.cs
services.AddHttpClient<GroqService>(); // Typed client
// or
services.AddSingleton<IHttpClientFactory>(); // Factory pattern
```

Both approaches handle the HttpClient lifecycle automatically:
- **Typed clients** - DI container manages the HttpClient
- **IHttpClientFactory** - Manages HttpClient pooling and lifetime
- **Result** - Services receiving injected HttpClients don't dispose them

### Quick Decision Guide

Ask yourself these questions:
1. **Did I create it with `new`?** → You own it → Implement IDisposable
2. **Was it passed to me (injected)?** → You don't own it → Don't dispose it
3. **Did I get it from a factory method?** → Check docs, but usually you own it → Implement IDisposable

### Common Patterns

**Dependency Injection (Don't Dispose):**
- Constructor parameters
- Properties set by DI container
- Services resolved from IServiceProvider

**You Own It (Must Dispose):**
- `new SomeDisposableType()`
- `File.Open()`, `File.Create()`
- `new HttpClient()` (avoid this, use DI instead)
- Factory methods that return new instances

## Quick Reference

### Do Implement IDisposable When:
- ✅ Your class owns IDisposable fields
- ✅ Your class allocates unmanaged memory
- ✅ Your class opens files, sockets, or database connections
- ✅ Your class subscribes to events from long-lived objects
- ✅ Your class creates timers or threads

### Don't Implement IDisposable When:
- ❌ Your class only contains value types (int, bool, etc.)
- ❌ Your class only references objects it doesn't own
- ❌ Your class is purely computational with no resources

### Using Statement Rules:
- ✅ Always use `using` for objects you create and own
- ✅ Use `await using` for IAsyncDisposable objects
- ❌ Don't use `using` on objects passed as parameters
- ❌ Don't return objects wrapped in `using`
- ✅ Prefer using declarations over using blocks in modern C#