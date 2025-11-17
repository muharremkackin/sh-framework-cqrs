# SH.Framework.Library.Cqrs

[![NuGet Version](https://img.shields.io/nuget/v/SH.Framework.Library.Cqrs.svg)](https://www.nuget.org/packages/SH.Framework.Library.Cqrs/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/SH.Framework.Library.Cqrs.svg)](https://www.nuget.org/packages/SH.Framework.Library.Cqrs/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A lightweight, high-performance library implementing the Command Query Responsibility Segregation (CQRS) pattern for .NET 10.0. This library promotes clean architecture and separation of concerns in modern .NET applications through well-defined interfaces and patterns.

## 🚀 Features

- **🎯 Request/Response Pattern**: Handle commands and queries with strongly-typed responses
- **📢 Event-Driven Architecture**: Publish and handle domain events asynchronously
- **🔄 Pipeline Behaviors**: Add cross-cutting concerns like validation, logging, caching, and authorization
- **💉 Dependency Injection Ready**: Seamless integration with Microsoft.Extensions.DependencyInjection
- **⚡ High Performance**: Optimized for minimal overhead and maximum throughput
- **🏗️ Clean Architecture**: Promotes separation of concerns and maintainable code
- **🔍 Auto-Discovery**: Automatic registration of handlers via assembly scanning
- **🛡️ Exception Handling**: Comprehensive error handling with custom exceptions
- **🔄 Cancellation Support**: Full support for cooperative cancellation throughout the pipeline

## 📦 Installation

```bash
dotnet add package SH.Framework.Library.Cqrs
```

## 🛠️ Quick Start

### 1. Register the Library

Add CQRS services to your dependency injection container during application startup:

```csharp
using SH.Framework.Library.Cqrs;

var builder = WebApplication.CreateBuilder(args);

// Register CQRS library with assembly scanning
builder.Services.AddCqrsLibraryConfiguration(
    Assembly.GetExecutingAssembly(),
    typeof(SomeHandlerInAnotherAssembly).Assembly
);

var app = builder.Build();
```

### 2. Define Requests and Handlers

**Command Example (No Response):**
```csharp
public record CreateUserCommand(string Name, string Email) : IRequest;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand>
{
    public async Task<Unit> HandleAsync(CreateUserCommand request, CancellationToken cancellationToken = default)
    {
        // Handle user creation logic
        await Task.CompletedTask;
        return Unit.Value;
    }
}
```

**Query Example (With Response):**
```csharp
public record GetUserQuery(int Id) : IRequest<UserDto>;

public record UserDto(int Id, string Name, string Email);

public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDto>
{
    public async Task<UserDto> HandleAsync(GetUserQuery request, CancellationToken cancellationToken = default)
    {
        // Fetch user from database
        return new UserDto(request.Id, "John Doe", "john@example.com");
    }
}
```

### 3. Use the Projector in Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IProjector _projector;

    public UsersController(IProjector projector)
    {
        _projector = projector;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(CreateUserCommand command, CancellationToken cancellationToken)
    {
        await _projector.SendAsync(command, cancellationToken);
        return Ok();
    }

    [HttpGet("{id}")]
    public async Task<UserDto> GetUser(int id, CancellationToken cancellationToken)
    {
        return await _projector.SendAsync(new GetUserQuery(id), cancellationToken);
    }
}
```

## 📡 Notifications (Domain Events)

Notifications enable event-driven architecture by allowing you to publish domain events that can be handled by multiple handlers asynchronously.

### 1. Define Notifications

```csharp
public record UserCreatedNotification(int UserId, string Name, string Email) : INotification;
```

### 2. Create Notification Handlers

```csharp
public class UserCreatedEmailHandler : INotificationHandler<UserCreatedNotification>
{
    public async Task HandleAsync(UserCreatedNotification notification, CancellationToken cancellationToken = default)
    {
        // Send welcome email
        await SendWelcomeEmailAsync(notification.Email);
    }
}

public class UserCreatedAuditHandler : INotificationHandler<UserCreatedNotification>
{
    public async Task HandleAsync(UserCreatedNotification notification, CancellationToken cancellationToken = default)
    {
        // Log user creation for audit purposes
        await LogUserCreationAsync(notification.UserId);
    }
}
```

### 3. Publish Notifications

```csharp
public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand>
{
    private readonly IProjector _projector;

    public CreateUserCommandHandler(IProjector projector)
    {
        _projector = projector;
    }

    public async Task<Unit> HandleAsync(CreateUserCommand request, CancellationToken cancellationToken = default)
    {
        // Create user logic
        var userId = await CreateUserAsync(request.Name, request.Email);
        
        // Publish domain event
        await _projector.PublishAsync(
            new UserCreatedNotification(userId, request.Name, request.Email), 
            cancellationToken);
        
        return Unit.Value;
    }
}
```

## 🔄 Pipeline Behaviors

Pipeline behaviors allow you to add cross-cutting concerns that execute before and after your request handlers. Common use cases include validation, logging, caching, and performance monitoring.

### 1. Create a Validation Behavior

```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> HandleAsync(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken = default)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));
            
            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .GroupBy(x => x.PropertyName, x => x.ErrorMessage)
                .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());

            if (failures.Any())
                throw new CqrsValidationException(failures);
        }

        return await next(cancellationToken);
    }
}
```

### 2. Create a Logging Behavior

```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> HandleAsync(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken = default)
    {
        var requestName = typeof(TRequest).Name;
        var requestId = request is IHasRequestId hasId ? hasId.RequestId : Guid.NewGuid();

        _logger.LogInformation("Handling request {RequestName} with ID {RequestId}", requestName, requestId);

        try
        {
            var response = await next(cancellationToken);
            _logger.LogInformation("Successfully handled request {RequestName} with ID {RequestId}", requestName, requestId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling request {RequestName} with ID {RequestId}", requestName, requestId);
            throw;
        }
    }
}
```

### 3. Register Behaviors

```csharp
// Register behaviors manually (they execute in reverse order - LIFO)
builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
// ValidationBehavior will execute first, then LoggingBehavior
```

## 📢 Notification Behaviors

Similar to request pipeline behaviors, notification behaviors allow you to add cross-cutting concerns to the notification publishing pipeline.

### 1. Create a Notification Logging Behavior

```csharp
public class NotificationLoggingBehavior<TNotification> : INotificationBehavior<TNotification>
    where TNotification : INotification
{
    private readonly ILogger<NotificationLoggingBehavior<TNotification>> _logger;

    public NotificationLoggingBehavior(ILogger<NotificationLoggingBehavior<TNotification>> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(
        TNotification notification, 
        NotificationHandlerDelegate next, 
        CancellationToken cancellationToken = default)
    {
        var notificationName = typeof(TNotification).Name;
        var notificationId = notification is IHasNotificationId hasId ? hasId.NotificationId : Guid.NewGuid();

        _logger.LogInformation("Publishing notification {NotificationName} with ID {NotificationId}", 
            notificationName, notificationId);

        try
        {
            await next(cancellationToken);
            _logger.LogInformation("Successfully published notification {NotificationName} with ID {NotificationId}", 
                notificationName, notificationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing notification {NotificationName} with ID {NotificationId}", 
                notificationName, notificationId);
            throw;
        }
    }
}
```

### 2. Register Notification Behaviors

```csharp
builder.Services.AddScoped(typeof(INotificationBehavior<>), typeof(NotificationLoggingBehavior<>));
```

## 🆔 Request and Notification Identification

The library provides interfaces for adding unique identifiers to requests and notifications for tracking and correlation purposes.

```csharp
public record CreateUserCommand(string Name, string Email, Guid RequestId) : IRequest, IHasRequestId;

public record UserCreatedNotification(int UserId, string Name, string Email, Guid NotificationId) 
    : INotification, IHasNotificationId;
```

## 🔧 API Reference

### IProjector Interface

The main entry point for sending requests and publishing notifications:

```csharp
public interface IProjector
{
    // Send request with typed response
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
    
    // Send request without response (returns Unit)
    Task SendAsync(IRequest request, CancellationToken cancellationToken = default);
    
    // Publish notification to all registered handlers
    Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}
```

### Core Interfaces

```csharp
// Request interfaces
public interface IRequest<out TResponse>
public interface IRequest : IRequest<Unit>

// Handler interfaces
public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
public interface IRequestHandler<in TRequest> : IRequestHandler<TRequest, Unit> where TRequest : IRequest<Unit>

// Notification interfaces
public interface INotification
public interface INotificationHandler<in TNotification> where TNotification : INotification

// Behavior interfaces
public interface IPipelineBehavior<in TRequest, TResponse> where TRequest : IRequest<TResponse>
public interface INotificationBehavior<in TNotification> where TNotification : INotification

// Identification interfaces
public interface IHasRequestId { Guid RequestId { get; } }
public interface IHasNotificationId { Guid NotificationId { get; } }
```

## ⚠️ Exception Handling

The library provides custom exceptions for different error scenarios:

### HandlerNotFoundException
Thrown when no handler is registered for a specific request type:
```csharp
try
{
    await projector.SendAsync(new UnregisteredCommand());
}
catch (HandlerNotFoundException ex)
{
    // ex.RequestType contains the type that had no handler
    Console.WriteLine($"No handler found for {ex.RequestType.Name}");
}
```

### MultipleHandlersFoundException
Thrown when multiple handlers are registered for the same request type:
```csharp
catch (MultipleHandlersFoundException ex)
{
    // ex.RequestType and ex.HandlerCount provide details
    Console.WriteLine($"Found {ex.HandlerCount} handlers for {ex.RequestType.Name}");
}
```

### CqrsValidationException
Thrown when validation fails (typically from a validation behavior):
```csharp
catch (CqrsValidationException ex)
{
    foreach (var error in ex.Errors)
    {
        Console.WriteLine($"{error.Key}: {string.Join(", ", error.Value)}");
    }
}
```

## 🚀 Performance Considerations

- **Lightweight Design**: Minimal allocations and overhead
- **Behavior Execution**: Behaviors execute in reverse registration order (LIFO)
- **Parallel Notifications**: Multiple notification handlers execute in parallel when possible
- **Cancellation Support**: Full cooperative cancellation support throughout the pipeline
- **DI Lifetimes**: Use appropriate lifetimes (typically Scoped for web applications)

## 🔄 Cancellation Support

All API methods support `CancellationToken` for cooperative cancellation:

```csharp
public async Task<IActionResult> CreateUser(CreateUserCommand command, CancellationToken cancellationToken)
{
    try
    {
        await _projector.SendAsync(command, cancellationToken);
        return Ok();
    }
    catch (OperationCanceledException)
    {
        return StatusCode(499); // Client Closed Request
    }
}
```

## 📚 Advanced Usage

### Custom Unit Type
The library uses a `Unit` struct for requests that don't return a value:

```csharp
public struct Unit { }
```

### Delegate Types
```csharp
public delegate Task NotificationHandlerDelegate(CancellationToken cancellationToken = default);
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellation = default);
```

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🏢 Company

**Strawhats Company**  
Created by Muharrem Kaçkın

---

⭐ If you find this library helpful, please consider giving it a star on GitHub!