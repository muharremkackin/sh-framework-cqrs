using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace SH.Framework.Library.Cqrs.Tests;

public class ProjectorTests
{
    private readonly IProjector _projector;

    public ProjectorTests()
    {
        var services = new ServiceCollection();
        
        services.AddCqrsLibraryConfiguration(Assembly.GetExecutingAssembly());

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(FirstBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(SecondBehavior<,>));
        
        IServiceProvider serviceProvider = services.BuildServiceProvider();
        _projector = serviceProvider.GetRequiredService<IProjector>();
    }
    
    #region Request/Response Tests

    [Fact]
    public async Task SendAsync_WithResponse_ShouldReturnCorrectResult()
    {
        var query = new PingQuery("Hello");

        var result = await _projector.SendAsync(query);
        
        result.Message.Should().Be("Pong: Hello");
    }

    [Fact]
    public async Task SendAsync_CommandNoResponse_ShouldExecuteSuccessfully()
    {
        var command = new SimpleCommand();
        var result = await _projector.SendAsync(command);
        
        result.Should().Be(Unit.Value);
    }

    [Fact]
    public async Task SendAsync_WithNoHandlerExists_ShouldThrowHandlerNotFoundException()
    {
        var unregistered = new UnregisteredRequest();

        Func<Task> act = async () => await _projector.SendAsync(unregistered);

        await act.Should().ThrowAsync<HandlerNotFoundException>()
            .Where(x => x.RequestType == typeof(UnregisteredRequest));
    }
    
    #endregion

    #region Pipeline Behavior Tests

    [Fact]
    public async Task SendAsync_ShouldExecuteBehaviorsInCorrectOrder()
    {
        var request = new BehaviorTestRequest();
        ExecutionTracker.Log.Clear();

        await _projector.SendAsync(request);

        ExecutionTracker.Log.Should().ContainInOrder(
            "First Start",
            "Second Start",
            "Handler Executed",
            "Second End",
            "First End"
        );
    }

    #endregion

    #region Notification Tests

    [Fact]
    public async Task PublishAsync_ShouldInvokeAllHandlers()
    {
        var notification = new PingNotification();
        NotificationTracker.Counter = 0;

        await _projector.PublishAsync(notification);
        
        NotificationTracker.Counter.Should().Be(2);
    }

    #endregion
    
    #region Test DTOs and Handlers
    // Request/Response
    public record PingQuery(string Content) : IRequest<PingResponse>;
    public record PingResponse(string Message);
    public class PingQueryHandler: IRequestHandler<PingQuery, PingResponse>
    {
        public Task<PingResponse> HandleAsync(PingQuery request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PingResponse($"Pong: {request.Content}"));
        }
    }

    // Command (Unit)
    public record SimpleCommand : IRequest;
    public class SimpleCommandHandler: IRequestHandler<SimpleCommand>
    {
        public Task<Unit> HandleAsync(SimpleCommand request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Unit.Value);
        }
    }
    
    // Unregistered
    public record UnregisteredRequest : IRequest;
    
    // Behaviors
    public static class ExecutionTracker
    {
        public static readonly List<string> Log = [];
    }

    public record BehaviorTestRequest : IRequest;
    public class BehaviorTestHandler: IRequestHandler<BehaviorTestRequest>
    {
        public Task<Unit> HandleAsync(BehaviorTestRequest request, CancellationToken cancellationToken = default)
        {
            ExecutionTracker.Log.Add("Handler Executed");
            return Task.FromResult(Unit.Value);
        }
    }
    
    public class FirstBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest: IRequest<TResponse>
    {
        public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken = default)
        {
            ExecutionTracker.Log.Add("First Start");
            var res = await next(cancellationToken);
            ExecutionTracker.Log.Add("First End");
            return res;
        }
    }
    
    public class SecondBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest: IRequest<TResponse>
    {
        public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken = default)
        {
            ExecutionTracker.Log.Add("Second Start");
            var res = await next(cancellationToken);
            ExecutionTracker.Log.Add("Second End");
            return res;
        }
    }
    
    public static class NotificationTracker
    {
        public static int Counter;
    }
    public record PingNotification : INotification;
    public class PingHandler1 : INotificationHandler<PingNotification>
    {
        public Task HandleAsync(PingNotification n, CancellationToken cancellationToken = default) 
        { 
            Interlocked.Increment(ref NotificationTracker.Counter); 
            return Task.CompletedTask; 
        }
    }
    public class PingHandler2 : INotificationHandler<PingNotification>
    {
        public Task HandleAsync(PingNotification n, CancellationToken cancellationToken = default) 
        { 
            Interlocked.Increment(ref NotificationTracker.Counter); 
            return Task.CompletedTask; 
        }
    }

    #endregion
}