using Domain.Attribute.ValueObjects;

namespace Domain.Attribute.Exceptions;

public sealed class AttributeTypeNotFoundException(AttributeTypeId attributeTypeId) : DomainException($"ویژگی با شناسه {attributeTypeId} یافت نشد.")
{
    public AttributeTypeId AttributeTypeId { get; } = attributeTypeId;

    public override string ErrorCode => "ATTRIBUTE_TYPE_NOT_FOUND";
}