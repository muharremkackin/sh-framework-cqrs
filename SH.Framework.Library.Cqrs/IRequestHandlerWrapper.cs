namespace SH.Framework.Library.Cqrs;

internal interface IRequestHandlerWrapper<TResponse>
{
    public Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider provider,
        CancellationToken cancellationToken = default);
}