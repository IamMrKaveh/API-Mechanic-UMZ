namespace Domain.Attribute.Exceptions;

public sealed class DuplicateAttributeValueException(string value, string attributeTypeName) : DomainException($"مقدار '{value}' قبلاً در ویژگی '{attributeTypeName}' وجود دارد.")
{
}