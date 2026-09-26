namespace Infrastructure.Persistence.Converters;

/// <summary>
/// Single generic EF Core converter for every Guid-backed strongly typed id.
/// Replaces the per-id *IdConverter subclasses.
/// </summary>
internal sealed class StronglyTypedIdConverter<TId> : ValueConverter<TId, Guid>
    where TId : StronglyTypedId<TId>
{
    public StronglyTypedIdConverter()
        : base(
            id => id.Value,
            value => StronglyTypedId<TId>.From(value))
    {
    }
}
