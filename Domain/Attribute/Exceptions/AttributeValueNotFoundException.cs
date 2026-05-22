using Domain.Attribute.ValueObjects;

namespace Domain.Attribute.Exceptions;

public sealed class AttributeValueNotFoundException(AttributeValueId attributeValueId) : DomainException($"مقدار ویژگی با شناسه {attributeValueId} یافت نشد.")
{
    public AttributeValueId AttributeValueId { get; } = attributeValueId;

    public override string ErrorCode => "ATTRIBUTE_VALUE_NOT_FOUND";
}