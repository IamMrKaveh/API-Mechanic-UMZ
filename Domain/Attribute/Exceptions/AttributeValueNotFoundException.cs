using Domain.Attribute.ValueObjects;
using Domain.Common.Exceptions;

namespace Domain.Attribute.Exceptions;

public sealed class AttributeValueNotFoundException : DomainException
{
    public AttributeValueId AttributeValueId { get; }

    public override string ErrorCode => "ATTRIBUTE_VALUE_NOT_FOUND";

    public AttributeValueNotFoundException(AttributeValueId attributeValueId)
        : base($"مقدار ویژگی با شناسه {attributeValueId} یافت نشد.")
    {
        AttributeValueId = attributeValueId;
    }
}