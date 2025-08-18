namespace SH.Framework.Library.Cqrs;

public interface IRequest<out TResponse>;

public interface IRequest : IRequest<Unit>;

public struct Unit
{
}

public delegate Task NotificationHandlerDelegate(CancellationToken cancellationToken = default);

public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellation = default);