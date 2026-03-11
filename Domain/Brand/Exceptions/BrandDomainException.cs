namespace Domain.Brand.Exceptions;

public sealed class BrandNotFoundException(Guid brandId) : Exception($"Brand with ID '{brandId}' was not found.")
{
}

public sealed class BrandNameAlreadyExistsException(string name) : Exception($"A brand with the name '{name}' already exists.")
{
}

public sealed class BrandAlreadyActiveException(Guid brandId) : Exception($"Brand with ID '{brandId}' is already active.")
{
}

public sealed class BrandAlreadyDeactivatedException(Guid brandId) : Exception($"Brand with ID '{brandId}' is already deactivated.")
{
}

public sealed class DeletedBrandMutationException(Guid brandId) : Exception($"Cannot modify deleted brand with ID '{brandId}'.")
{
}