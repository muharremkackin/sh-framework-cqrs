using Microsoft.Extensions.DependencyInjection;

namespace SH.Framework.Library.Cqrs;

internal sealed class NotificationHandlerWrapperImplementation<TNotification>: INotificationHandlerWrapper where TNotification : INotification
{
    public Task Handle(INotification notification, IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        var handlers = provider.GetServices<INotificationHandler<TNotification>>();
        var behaviors = provider.GetServices<INotificationBehavior<TNotification>>().Reverse().ToList();

        NotificationHandlerDelegate next = ct =>
        {
            var tasks = handlers.Select(async handler =>
            {
                try
                {
                    await handler.HandleAsync((TNotification)notification, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Notification handler failed: {ex.Message}");
                }
            });
            return Task.WhenAll(tasks);
        };
        
        foreach (var behavior in behaviors)
        {
            var currentNext = next;
            next = ct => behavior.HandleAsync((TNotification)notification, currentNext, ct);
        }

        return next(cancellationToken);
    }
}