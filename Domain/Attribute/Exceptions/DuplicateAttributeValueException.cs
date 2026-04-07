using Domain.Common.Exceptions;

namespace Domain.Attribute.Exceptions;

public sealed class DuplicateAttributeValueException : DomainException
{
    public string Name { get; }

    public override string ErrorCode => "DUPLICATE_ATTRIBUTE_VALUE";

    public DuplicateAttributeValueException(string name)
        : base($"مقدار ویژگی با نام '{name}' قبلاً وجود دارد.")
    {
        Name = name;
    }
}