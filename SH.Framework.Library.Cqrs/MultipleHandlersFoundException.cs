namespace SH.Framework.Library.Cqrs;

/// <summary>
/// Exception that is thrown when multiple handlers are found for a specific request type in the CQRS framework.
/// </summary>
public class MultipleHandlersFoundException : Exception
{
    /// <summary>
    /// Gets the type of the request that caused the exception due to multiple handlers being found.
    /// </summary>
    public Type RequestType { get; }

    /// <summary>
    /// Gets the number of handlers associated with the request type that caused the exception.
    /// </summary>
    public int HandlerCount { get; }

    /// <summary>
    /// Exception that is thrown when multiple handlers are found for a specific request type in the CQRS framework.
    /// </summary>
    public MultipleHandlersFoundException(Type requestType, int handlerCount)
        : base($"Multiple handlers ({handlerCount}) found for request type: {requestType.Name}")
    {
        RequestType = requestType;
        HandlerCount = handlerCount;
    }

    /// <summary>
    /// Exception that is thrown when multiple handlers are found for a specific request type in the CQRS framework.
    /// </summary>
    public MultipleHandlersFoundException(Type requestType, int handlerCount, string message)
        : base(message)
    {
        RequestType = requestType;
        HandlerCount = handlerCount;
    }

    /// <summary>
    /// Exception that is thrown when multiple handlers are found for a specific request type in the CQRS framework.
    /// </summary>
    public MultipleHandlersFoundException(Type requestType, int handlerCount, string message, Exception innerException)
        : base(message, innerException)
    {
        RequestType = requestType;
        HandlerCount = handlerCount;
    }
}