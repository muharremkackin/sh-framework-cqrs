namespace SH.Framework.Library.Cqrs;

/// <summary>
/// Exception type representing one or more validation failures detected during a CQRS request lifecycle.
/// Carries a structured collection of validation errors keyed by property/field name.
/// </summary>
public class CqrsValidationException : Exception
{
    /// <summary>
    /// Gets the collection of validation errors where the key is the property name
    /// and the value is an array of error messages associated with that property.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CqrsValidationException"/> class
    /// with a default message and the specified validation errors.
    /// </summary>
    /// <param name="errors">A dictionary of validation errors keyed by the property name.</param>
    public CqrsValidationException(Dictionary<string, string[]> errors) 
        : base("One or more validation errors occurred.")
    {
        Errors = errors.AsReadOnly();
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CqrsValidationException"/> class
    /// with a custom error message and the specified validation errors.
    /// </summary>
    /// <param name="message">The custom error message describing the validation failure.</param>
    /// <param name="errors">A dictionary of validation errors keyed by the property name.</param>
    public CqrsValidationException(string message, Dictionary<string, string[]> errors) 
        : base(message)
    {
        Errors = errors.AsReadOnly();
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CqrsValidationException"/> class
    /// with a custom error message, the specified validation errors, and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The custom error message describing the validation failure.</param>
    /// <param name="errors">A dictionary of validation errors keyed by the property name.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public CqrsValidationException(string message, Dictionary<string, string[]> errors, Exception innerException) 
        : base(message, innerException)
    {
        Errors = errors.AsReadOnly();
    }
}