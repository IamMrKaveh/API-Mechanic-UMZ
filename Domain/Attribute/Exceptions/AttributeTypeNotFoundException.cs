using Domain.Attribute.ValueObjects;
using SharedKernel.Exceptions;

namespace Domain.Attribute.Exceptions;

public sealed class AttributeTypeNotFoundException(AttributeTypeId attributeTypeId)
    : NotFoundException<AttributeTypeId>(
        "ATTRIBUTE_TYPE_NOT_FOUND",
        attributeTypeId,
        $"ویژگی با شناسه {attributeTypeId} یافت نشد.")
{
    public AttributeTypeId AttributeTypeId => Value;
}
