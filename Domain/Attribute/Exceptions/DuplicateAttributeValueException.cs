using SharedKernel.Exceptions;

namespace Domain.Attribute.Exceptions;

public sealed class DuplicateAttributeValueException(string name)
    : AlreadyExistsException<string>(
        "DUPLICATE_ATTRIBUTE_VALUE",
        name,
        $"مقدار ویژگی با نام '{name}' قبلاً وجود دارد.")
{
    public string Name => Value;
}
