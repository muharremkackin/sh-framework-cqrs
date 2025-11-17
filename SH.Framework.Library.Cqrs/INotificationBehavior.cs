namespace SH.Framework.Library.Cqrs;

public interface INotificationBehavior<in TNotification> where TNotification : INotification
{
    Task HandleAsync(TNotification notification, NotificationHandlerDelegate next,
        CancellationToken cancellationToken = default);
}