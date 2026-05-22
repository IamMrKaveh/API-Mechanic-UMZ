namespace Domain.Attribute.Exceptions;

public sealed class DuplicateAttributeValueException(string name) : DomainException($"مقدار ویژگی با نام '{name}' قبلاً وجود دارد.")
{
    public string Name { get; } = name;

    public override string ErrorCode => "DUPLICATE_ATTRIBUTE_VALUE";
}