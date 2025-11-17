using Microsoft.Extensions.DependencyInjection;

namespace SH.Framework.Library.Cqrs;

public class Projector(IServiceProvider provider) : IProjector
{
    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        
        var requestType = request.GetType();
        var responseType = typeof(TResponse);

        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, responseType);
        var behaviors = provider.GetServices(behaviorType).Cast<object>().Reverse();
        RequestHandlerDelegate<TResponse> handler = ct => HandleRequestCore<TResponse>(request, ct);
        foreach (var behavior in behaviors)
        {
            var currentHandler = handler;
            handler = ct =>
            {
                var method = behaviorType.GetMethod("HandleAsync");
                var result = method!.Invoke(behavior, [request, currentHandler, cancellationToken]);
                return (Task<TResponse>)result!;
            };
        }

        return await handler(cancellationToken);
    }

    public async Task SendAsync(IRequest request, CancellationToken cancellationToken = default)
    {
        await SendAsync<Unit>(request, cancellationToken);
    }

    public async Task PublishAsync<TNotification>(TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);
        cancellationToken.ThrowIfCancellationRequested();
        
        var notificationType = notification.GetType();
        var behaviorType = typeof(INotificationBehavior<>).MakeGenericType(notificationType);
        var behaviors = provider.GetServices(behaviorType).Cast<object>().Reverse();

        NotificationHandlerDelegate handler = ct => HandleNotificationCore(notification, ct);
        foreach (var behavior in behaviors)
        {
            var currentHandler = handler;
            handler = ct =>
            {
                var method = behaviorType.GetMethod("HandleAsync");
                var result = method!.Invoke(behavior, [notification, currentHandler, ct]);
                return (Task)result!;
            };
        }

        await handler(cancellationToken);
    }

    private async Task<TResponse> HandleRequestCore<TResponse>(IRequest<TResponse> request,
        CancellationToken cancellationToken)
    {
        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));

        var handlers = provider.GetServices(handlerType).ToList();
        switch (handlers.Count)
        {
            case 0:
                throw new HandlerNotFoundException(requestType);
            case > 1:
                throw new MultipleHandlersFoundException(requestType, handlers.Count);
        }

        var handler = handlers.First();
        var method = handlerType.GetMethod("HandleAsync");

        var result = method!.Invoke(handler, [request, cancellationToken]);
        return await (Task<TResponse>)result!;
    }

    private async Task HandleNotificationCore<TNotification>(TNotification notification,
        CancellationToken cancellationToken) where TNotification : INotification
    {
        var notificationType = notification.GetType();
        var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
        var handlers = provider.GetServices(handlerType);
        var tasks = handlers.Select(async handler =>
        {
            try
            {
                var method = handlerType.GetMethod("HandleAsync");
                var result = method!.Invoke(handler, [notification, cancellationToken]);
                await (Task)result!;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Notification handler failed: {ex.Message}");
            }
        });

        await Task.WhenAll(tasks);
    }
}