using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace SH.Framework.Library.Cqrs;

public static class RegisterCqrsLibrary
{
    public static void AddCqrsLibraryConfiguration(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddCqrsLibraryConfiguration(assemblies, ServiceLifetime.Scoped);
    }
    public static void AddCqrsLibraryConfiguration(this IServiceCollection services, Assembly[] assemblies, ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        services.AddScoped<IProjector, Projector>();
        foreach (var assembly in assemblies)
        {
            services.RegisterRequestHandlers(assembly, lifetime);
            services.RegisterNotificationHandlers(assembly, lifetime);
        }
    }

    private static void RegisterRequestHandlers(this IServiceCollection services, Assembly assembly, ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType &&
                                                   (i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>) ||
                                                    i.GetGenericTypeDefinition() == typeof(IRequestHandler<>))))
            .Where(t => t is { IsInterface: false, IsAbstract: false });

        foreach (var handlerType in handlerTypes)
        {
            var interfaces = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType &&
                            (i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>) ||
                             i.GetGenericTypeDefinition() == typeof(IRequestHandler<>)))
                .ToList();

            foreach (var @interface in interfaces) services.Add(new ServiceDescriptor(@interface, handlerType, lifetime));
        }
    }

    private static void RegisterNotificationHandlers(this IServiceCollection services, Assembly assembly, ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType &&
                                                   i.GetGenericTypeDefinition() == typeof(INotificationHandler<>)))
            .Where(t => t is { IsInterface: false, IsAbstract: false });

        foreach (var handlerType in handlerTypes)
        {
            var interfaces = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType &&
                            i.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
                .ToList();

            foreach (var @interface in interfaces) services.Add(new ServiceDescriptor(@interface, handlerType, lifetime));
        }
    }
}