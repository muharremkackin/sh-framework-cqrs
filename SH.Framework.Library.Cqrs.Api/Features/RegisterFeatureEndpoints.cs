using SH.Framework.Library.Cqrs.Api.Features.Home;

namespace SH.Framework.Library.Cqrs.Api.Features;

public static class RegisterFeatureEndpoints
{
    public static void MapFeatureEndpoints(this WebApplication app)
    {
        HomeIndexQuery.Endpoint(app);
    }
}