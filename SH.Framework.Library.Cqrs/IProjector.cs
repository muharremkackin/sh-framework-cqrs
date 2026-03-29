namespace SH.Framework.Library.Cqrs;

public interface IProjector
{
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
    Task<Unit> SendAsync(IRequest request, CancellationToken cancellationToken = default);
    TResponse Send<TResponse>(IRequest<TResponse> request);
    Unit Send(IRequest request);
    Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;
    void Publish<TNotification>(TNotification notification) where TNotification : INotification;
}