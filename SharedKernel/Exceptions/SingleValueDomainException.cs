namespace SharedKernel.Exceptions;

/// <summary>
/// Base class for domain exceptions that carry a single value (usually an identifier).
/// Eliminates the repeated pattern of: single-id ctor + message containing the id
/// + typed property for the id + <see cref="DomainException.ErrorCode"/> override.
/// The error code is passed via the base <see cref="DomainException"/> ctor,
/// so derived types no longer need to override <see cref="DomainException.ErrorCode"/>.
/// </summary>
/// <typeparam name="TValue">Type of the carried value (usually a strongly-typed id).</typeparam>
public abstract class SingleValueDomainException<TValue> : DomainException
{
    protected SingleValueDomainException(
        string errorCode,
        TValue value,
        string message,
        IReadOnlyDictionary<string, object?>? args = null,
        Exception? innerException = null)
        : base(errorCode, message, args, innerException)
    {
        Value = value;
    }

    /// <summary>
    /// The single value carried by the exception (usually the identifier).
    /// Derived types expose it under a domain-specific name (e.g. BrandId, VariantId).
    /// </summary>
    public TValue Value { get; }
}

/// <summary>
/// Base class for "not found" domain exceptions carrying the missing identifier.
/// </summary>
/// <typeparam name="TId">Type of the missing identifier.</typeparam>
public abstract class NotFoundException<TId> : SingleValueDomainException<TId>
{
    protected NotFoundException(
        string errorCode,
        TId id,
        string message,
        IReadOnlyDictionary<string, object?>? args = null,
        Exception? innerException = null)
        : base(errorCode, id, message, args, innerException)
    {
    }

    /// <summary>
    /// Alias for <see cref="SingleValueDomainException{TValue}.Value"/> with not-found semantics.
    /// </summary>
    public TId Id => Value;
}

/// <summary>
/// Base class for "already exists / duplicate" domain exceptions carrying the conflicting value.
/// </summary>
/// <typeparam name="TValue">Type of the conflicting value (id, name, ...).</typeparam>
public abstract class AlreadyExistsException<TValue> : SingleValueDomainException<TValue>
{
    protected AlreadyExistsException(
        string errorCode,
        TValue value,
        string message,
        IReadOnlyDictionary<string, object?>? args = null,
        Exception? innerException = null)
        : base(errorCode, value, message, args, innerException)
    {
    }
}

/// <summary>
/// Base class for "conflict with current state" domain exceptions
/// (e.g. AlreadyActive, AlreadyCheckedOut, AlreadyVerified) carrying a single value.
/// </summary>
/// <typeparam name="TValue">Type of the carried value (usually an identifier).</typeparam>
public abstract class ConflictException<TValue> : SingleValueDomainException<TValue>
{
    protected ConflictException(
        string errorCode,
        TValue value,
        string message,
        IReadOnlyDictionary<string, object?>? args = null,
        Exception? innerException = null)
        : base(errorCode, value, message, args, innerException)
    {
    }
}
