namespace SH.Framework.Library.Cqrs;

internal interface INotificationHandlerWrapper
{
    public Task Handle(INotification notification, IServiceProvider provider, CancellationToken cancellationToken = default);
    
}