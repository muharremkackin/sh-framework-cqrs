# SH.Framework.Library.Cqrs

A lightweight and high-performance library implementing the Command Query Responsibility Segregation (CQRS) pattern for .NET 9.0. Provides clean architecture and separated responsibilities in modern .NET applications with support for Request/Response, Notifications, and Pipeline Behaviors.

## Features

- **Request/Response Pattern**: Handle commands and queries with typed responses
- **Notification System**: Publish and handle domain events asynchronously
- **Pipeline Behaviors**: Add cross-cutting concerns like validation, logging, and caching
- **Dependency Injection Integration**: Seamless integration with Microsoft.Extensions.DependencyInjection
- **High Performance**: Optimized for minimal overhead and maximum throughput
- **Clean Architecture**: Promotes separation of concerns and maintainable code

## Installation

```bash
dotnet add package SH.Framework.Library.Cqrs
```

## Quick Start

### 1. Register the Library

```csharp
using SH.Framework.Library.Cqrs;

var builder = WebApplication.CreateBuilder(args);

// Register CQRS library with assemblies containing handlers
builder.Services.AddCqrsLibraryConfiguration(
    Assembly.GetExecutingAssembly(),
    typeof(MyHandler).Assembly
);

var app = builder.Build();
```

### 2. Create a Command/Query

```csharp
// Command (no return value)
public record CreateUserCommand(string Name, string Email) : IRequest;

// Query (with return value)
public record GetUserQuery(int Id) : IRequest<UserDto>;

// Response DTO
public record UserDto(int Id, string Name, string Email);
```

### 3. Create Handlers

```csharp
// Command Handler
public class CreateUserHandler : IRequestHandler<CreateUserCommand>
{
    private readonly IUserRepository _repository;

    public CreateUserHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<Unit> HandleAsync(CreateUserCommand request, 
        CancellationToken cancellationToken = default)
    {
        var user = new User(request.Name, request.Email);
        await _repository.SaveAsync(user);
        return Unit.Value;
    }
}

// Query Handler
public class GetUserHandler : IRequestHandler<GetUserQuery, UserDto>
{
    private readonly IUserRepository _repository;

    public GetUserHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<UserDto> HandleAsync(GetUserQuery request, 
        CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByIdAsync(request.Id);
        return new UserDto(user.Id, user.Name, user.Email);
    }
}
```

### 4. Use the Projector

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
    public async Task<IActionResult> CreateUser(CreateUserCommand command)
    {
        await _projector.SendAsync(command);
        return Ok();
    }

    [HttpGet("{id}")]
    public async Task<UserDto> GetUser(int id)
    {
        return await _projector.SendAsync(new GetUserQuery(id));
    }
}
```

## Notifications (Domain Events)

### 1. Create a Notification

```csharp
public record UserCreatedNotification(int UserId, string Name, string Email) : INotification;
```

### 2. Create Notification Handlers

```csharp
// Email notification handler
public class SendWelcomeEmailHandler : INotificationHandler<UserCreatedNotification>
{
    private readonly IEmailService _emailService;

    public SendWelcomeEmailHandler(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task HandleAsync(UserCreatedNotification notification, 
        CancellationToken cancellationToken = default)
    {
        await _emailService.SendWelcomeEmailAsync(notification.Email, notification.Name);
    }
}

// Audit log handler
public class LogUserCreationHandler : INotificationHandler<UserCreatedNotification>
{
    private readonly ILogger<LogUserCreationHandler> _logger;

    public LogUserCreationHandler(ILogger<LogUserCreationHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(UserCreatedNotification notification, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("User created: {UserId} - {Name}", 
            notification.UserId, notification.Name);
    }
}
```

### 3. Publish Notifications

```csharp
public class CreateUserHandler : IRequestHandler<CreateUserCommand>
{
    private readonly IUserRepository _repository;
    private readonly IProjector _projector;

    public CreateUserHandler(IUserRepository repository, IProjector projector)
    {
        _repository = repository;
        _projector = projector;
    }

    public async Task<Unit> HandleAsync(CreateUserCommand request, 
        CancellationToken cancellationToken = default)
    {
        var user = new User(request.Name, request.Email);
        await _repository.SaveAsync(user);

        // Publish notification
        var notification = new UserCreatedNotification(user.Id, user.Name, user.Email);
        await _projector.PublishAsync(notification, cancellationToken);

        return Unit.Value;
    }
}
```

## Pipeline Behaviors

Pipeline behaviors allow you to add cross-cutting concerns like validation, logging, caching, etc.

### 1. Create a Validation Behavior

```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IValidator<TRequest>? _validator;

    public ValidationBehavior(IValidator<TRequest>? validator = null)
    {
        _validator = validator;
    }

    public async Task<TResponse> HandleAsync(TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken = default)
    {
        if (_validator != null)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }
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

    public async Task<TResponse> HandleAsync(TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken = default)
    {
        var requestName = typeof(TRequest).Name;
        
        _logger.LogInformation("Handling {RequestName}", requestName);
        
        var stopwatch = Stopwatch.StartNew();
        var response = await next(cancellationToken);
        stopwatch.Stop();

        _logger.LogInformation("Handled {RequestName} in {ElapsedMs}ms", 
            requestName, stopwatch.ElapsedMilliseconds);

        return response;
    }
}
```

### 3. Register Behaviors

**Note**: You need to manually register pipeline behaviors in your DI container:

```csharp
builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
```

## Notification Behaviors

Similar to pipeline behaviors, but for notifications:

```csharp
public class NotificationLoggingBehavior<TNotification> : INotificationBehavior<TNotification>
    where TNotification : INotification
{
    private readonly ILogger<NotificationLoggingBehavior<TNotification>> _logger;

    public NotificationLoggingBehavior(ILogger<NotificationLoggingBehavior<TNotification>> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(TNotification notification, 
        NotificationHandlerDelegate next, 
        CancellationToken cancellationToken = default)
    {
        var notificationName = typeof(TNotification).Name;
        _logger.LogInformation("Publishing {NotificationName}", notificationName);

        await next(cancellationToken);

        _logger.LogInformation("Published {NotificationName}", notificationName);
    }
}
```

## Request and Notification IDs

The library provides interfaces for tracking requests and notifications:

```csharp
public record CreateUserCommand(string Name, string Email) : IRequest, IHasRequestId
{
    public Guid RequestId { get; init; } = Guid.NewGuid();
}

public record UserCreatedNotification(int UserId, string Name, string Email) : INotification, IHasNotificationId
{
    public Guid NotificationId { get; init; } = Guid.NewGuid();
}
```

## Error Handling

Notification handlers include built-in error handling - if one handler fails, others will still execute:

```csharp
// In the Projector class, notification handlers are wrapped in try-catch
private async Task HandleNotificationCore<TNotification>(TNotification notification,
    CancellationToken cancellationToken) where TNotification : INotification
{
    // ... handlers execution with error handling
    var tasks = handlers.Select(async handler =>
    {
        try
        {
            // Handler execution
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Notification handler failed: {ex.Message}");
            // Handler failure doesn't stop other handlers
        }
    });
}
```

## Best Practices

1. **Keep Handlers Simple**: Each handler should have a single responsibility
2. **Use Notifications for Side Effects**: Commands should focus on the main operation, use notifications for side effects
3. **Validate Early**: Use pipeline behaviors for validation before reaching handlers
4. **Log Appropriately**: Use logging behaviors to track request execution
5. **Handle Errors Gracefully**: Implement proper error handling in your handlers
6. **Use Cancellation Tokens**: Always pass and respect cancellation tokens

## Performance Considerations

- The library is designed for high performance with minimal allocations
- Behaviors are executed in reverse order of registration (LIFO)
- Notification handlers execute in parallel when possible
- Use appropriate DI lifetimes (typically Scoped for web applications)

## Requirements

- .NET 9.0 or later
- Microsoft.Extensions.DependencyInjection 9.0.8 or later

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests on GitHub.

## License

This project is licensed under the MIT License - see the [LICENSE](https://opensource.org/licenses/MIT) file for details.

## Repository

[https://github.com/muharremkackin/sh-framework-cqrs](https://github.com/muharremkackin/sh-framework-cqrs)