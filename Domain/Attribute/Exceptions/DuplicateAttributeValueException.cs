namespace Domain.Attribute.Exceptions;

public sealed class DuplicateAttributeValueException : DomainException
{
    public DuplicateAttributeValueException(string value, string attributeTypeName)
        : base($"مقدار '{value}' قبلاً در ویژگی '{attributeTypeName}' وجود دارد.") { }
}