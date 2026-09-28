using SharedKernel.Exceptions;

namespace Domain.Attribute.Exceptions;

public sealed class DuplicateAttributeException(string name)
    : AlreadyExistsException<string>(
        "DUPLICATE_ATTRIBUTE",
        name,
        $"ویژگی با نام '{name}' قبلاً وجود دارد.")
{
    public string Name => Value;
}
