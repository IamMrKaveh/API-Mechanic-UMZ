using System.Reflection;
using SharedKernel.Exceptions;

namespace SharedKernel.ValueObjects;

/// <summary>
/// Base record for all Guid-backed strongly typed identifiers.
/// Removes the need to repeat the private ctor / NewId / From / ToString /
/// implicit operator skeleton in every *Id value object.
/// </summary>
public abstract record StronglyTypedId<TSelf> : IStronglyTypedId
    where TSelf : StronglyTypedId<TSelf>
{
    public Guid Value { get; }

    protected StronglyTypedId(Guid value)
    {
        if (value == Guid.Empty)
            throw new DomainException($"{typeof(TSelf).Name} cannot be empty.");
        Value = value;
    }

    public static TSelf NewId() => From(Guid.NewGuid());

    public static TSelf From(Guid value)
    {
        if (value == Guid.Empty)
            throw new DomainException($"{typeof(TSelf).Name} cannot be empty.");
        return (TSelf)Activator.CreateInstance(
            typeof(TSelf),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: [value],
            culture: null)!;
    }

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(StronglyTypedId<TSelf> id) => id.Value;
}
