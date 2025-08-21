namespace SH.Framework.Library.Cqrs.Api.Behaviors;

public class ExceptionHandlerBehavior<TRequest, TResponse>: IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken = default)
    {
        try
        {
            return await next(cancellationToken);
            
        } catch (Exception e)
        {
            Console.WriteLine(e);
            throw;   
        }
    }
}