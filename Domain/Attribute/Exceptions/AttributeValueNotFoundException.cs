using Domain.Attribute.ValueObjects;
using SharedKernel.Exceptions;

namespace Domain.Attribute.Exceptions;

public sealed class AttributeValueNotFoundException(AttributeValueId attributeValueId)
    : NotFoundException<AttributeValueId>(
        "ATTRIBUTE_VALUE_NOT_FOUND",
        attributeValueId,
        $"مقدار ویژگی با شناسه {attributeValueId} یافت نشد.")
{
    public AttributeValueId AttributeValueId => Value;
}
