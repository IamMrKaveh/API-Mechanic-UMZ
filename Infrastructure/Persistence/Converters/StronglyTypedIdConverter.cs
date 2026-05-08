namespace Infrastructure.Persistence.Converters;

internal abstract class StronglyTypedIdConverter<TId>(
    Func<Guid, TId> factory)
    : ValueConverter<TId, Guid>(
        id => id.Value,
        value => factory(value))
    where TId : class, IStronglyTypedId
{
}