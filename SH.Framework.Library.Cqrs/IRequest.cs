namespace SH.Framework.Library.Cqrs;

public interface IRequest<out TResponse>;

public interface IRequest : IRequest<Unit>;

public struct Unit: IEquatable<Unit>
{
    public bool Equals(Unit other) => true;

    public override bool Equals(object? obj)
    {
        return obj is Unit other && Equals(other);
    }

    public override int GetHashCode() => 0;
    
    public static bool operator ==(Unit left, Unit right) => true;
    public static bool operator !=(Unit left, Unit right) => false;
    
    public static Unit Value => default;
}

public delegate Task NotificationHandlerDelegate(CancellationToken cancellationToken = default);

public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellation = default);