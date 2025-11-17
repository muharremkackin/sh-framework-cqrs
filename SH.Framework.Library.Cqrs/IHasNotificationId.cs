namespace SH.Framework.Library.Cqrs;

public interface IHasNotificationId
{
    public Guid NotificationId { get; }
}