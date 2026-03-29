using Microsoft.Extensions.DependencyInjection;

namespace SH.Framework.Library.Cqrs;

internal sealed class RequestHandlerWrapperImplementation<TRequest, TResponse>: IRequestHandlerWrapper<TResponse> where TRequest: IRequest<TResponse>
{
    public Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        var handlers = provider.GetServices<IRequestHandler<TRequest, TResponse>>().ToList();

        if (handlers.Count == 0)
            throw new HandlerNotFoundException(typeof(TRequest));
        if (handlers.Count > 1)
            throw new MultipleHandlersFoundException(typeof(TRequest), handlers.Count);

        var handler = handlers[0];

        var behaviors = provider.GetServices<IPipelineBehavior<TRequest, TResponse>>().Reverse().ToList();
        
        RequestHandlerDelegate<TResponse> next = cancellationToken => handler.HandleAsync((TRequest)request, cancellationToken);

        foreach (var behavior in behaviors)
        {
            var currentNext = next;
            next = cancellationToken => behavior.HandleAsync((TRequest)request, currentNext, cancellationToken);
        }

        return next(cancellationToken);
    }
}