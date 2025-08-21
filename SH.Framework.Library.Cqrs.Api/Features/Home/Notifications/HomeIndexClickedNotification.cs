namespace SH.Framework.Library.Cqrs.Api.Features.Home.Notifications;

public class HomeIndexClickedNotification
{
    public record Notification(): INotification;
    
    public class Handler: INotificationHandler<Notification>
    {
        public Task HandleAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            Console.WriteLine("HomeIndexClickedNotification");
            return Task.CompletedTask;
        }
    }
}