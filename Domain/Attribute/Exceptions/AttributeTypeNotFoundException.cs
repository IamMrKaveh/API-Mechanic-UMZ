using Domain.Attribute.ValueObjects;

namespace Domain.Attribute.Exceptions;

public sealed class AttributeTypeNotFoundException : DomainException
{
    public AttributeTypeId AttributeTypeId { get; }

    public override string ErrorCode => "ATTRIBUTE_TYPE_NOT_FOUND";

    public AttributeTypeNotFoundException(AttributeTypeId attributeTypeId)
        : base($"ویژگی با شناسه {attributeTypeId} یافت نشد.")
    {
        AttributeTypeId = attributeTypeId;
    }
}