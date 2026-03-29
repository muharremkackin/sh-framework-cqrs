using System.Collections.Concurrent;

namespace SH.Framework.Library.Cqrs;

public sealed class Projector(IServiceProvider provider) : IProjector
{
    private static readonly ConcurrentDictionary<Type, object> RequestWrappers = new();
    private static readonly ConcurrentDictionary<Type, INotificationHandlerWrapper> NotificationWrappers = new();
    
    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        
        var requestType = request.GetType();
        var wrapper = (IRequestHandlerWrapper<TResponse>)RequestWrappers.GetOrAdd(requestType, t =>
        {
            var wrapperType = typeof(RequestHandlerWrapperImplementation<,>).MakeGenericType(t, typeof(TResponse));

            return Activator.CreateInstance(wrapperType)!;
        });

        return await wrapper.Handle(request, provider, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Unit> SendAsync(IRequest request, CancellationToken cancellationToken = default)
    {
        return await SendAsync<Unit>(request, cancellationToken).ConfigureAwait(false);
    }

    public TResponse Send<TResponse>(IRequest<TResponse> request)
    {
        return SendAsync(request).GetAwaiter().GetResult();
    }

    public Unit Send(IRequest request)
    {
        return SendAsync(request).GetAwaiter().GetResult();
    }

    public async Task PublishAsync<TNotification>(TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);
        cancellationToken.ThrowIfCancellationRequested();
        
        var notificationType = notification.GetType();
        var wrapper = NotificationWrappers.GetOrAdd(notificationType, t =>
        {
            var wrapperType = typeof(NotificationHandlerWrapperImplementation<>).MakeGenericType(t);
            return (INotificationHandlerWrapper)Activator.CreateInstance(wrapperType)!;
        });
        
        await wrapper.Handle(notification, provider, cancellationToken).ConfigureAwait(false);
    }

    public void Publish<TNotification>(TNotification notification) where TNotification : INotification
    {
        PublishAsync(notification).GetAwaiter().GetResult();
    }
}