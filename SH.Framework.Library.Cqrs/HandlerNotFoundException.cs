namespace SH.Framework.Library.Cqrs;

/// <summary>
/// Represents an exception thrown when no handler is found for a specific request type in the CQRS framework.
/// </summary>
public class HandlerNotFoundException: Exception
{
    /// <summary>
    /// Gets the type of the request for which no handler was found.
    /// </summary>
    public Type RequestType { get; }

    /// <summary>
    /// Represents an exception thrown when a handler for a specific request type cannot be found.
    /// </summary>
    public HandlerNotFoundException(Type requestType)
        : base($"No handler found for request type: {requestType.Name}")
    {
        RequestType = requestType;
    }

    /// <summary>
    /// Represents an exception thrown when a handler for a specific request type cannot be found.
    /// </summary>
    public HandlerNotFoundException(Type requestType, string message)
        : base(message)
    {
        RequestType = requestType;
    }

    /// <summary>
    /// Represents an exception thrown when a handler for a specific request type cannot be found.
    /// </summary>
    public HandlerNotFoundException(Type requestType, string message, Exception innerException)
        : base(message, innerException)
    {
        RequestType = requestType;
    }
}