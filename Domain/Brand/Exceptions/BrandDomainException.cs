using Domain.Brand.ValueObjects;

namespace Domain.Brand.Exceptions;

public sealed class BrandNotFoundException(BrandId brandId) : Exception($"Brand with ID '{brandId}' was not found.")
{
}

public sealed class BrandNameAlreadyExistsException(BrandName name) : Exception($"A brand with the name '{name}' already exists.")
{
}

public sealed class BrandAlreadyActiveException(BrandId brandId) : Exception($"Brand with ID '{brandId}' is already active.")
{
}

public sealed class BrandAlreadyDeactivatedException(BrandId brandId) : Exception($"Brand with ID '{brandId}' is already deactivated.")
{
}

public sealed class DeletedBrandMutationException(BrandId brandId) : Exception($"Cannot modify deleted brand with ID '{brandId}'.")
{
}