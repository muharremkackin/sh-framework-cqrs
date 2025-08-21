using Microsoft.AspNetCore.Mvc;
using SH.Framework.Library.Cqrs.Api.Features.Home.Notifications;

namespace SH.Framework.Library.Cqrs.Api.Features.Home;

public class HomeIndexQuery
{
    public record Query(): IRequest<string>;
    
    public class Handler(IProjector projector): IRequestHandler<Query, string>
    {
        public async Task<string> HandleAsync(Query request, CancellationToken cancellationToken = default)
        {
            await projector.PublishAsync(new HomeIndexClickedNotification.Notification(), cancellationToken);
            return "Healthy";
        }
    }

    public static void Endpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/",
            async ([AsParameters] Query query, [FromServices] IProjector projector,
                CancellationToken cancellationToken) =>
            {
                var result = await projector.SendAsync(query, cancellationToken);
                return Results.Ok(result);
            });
    }
}