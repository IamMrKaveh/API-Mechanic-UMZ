using Domain.Attribute.ValueObjects;

namespace Domain.Attribute.Exceptions;

public sealed class AttributeValueNotFoundException(AttributeValueId id) : DomainException($"مقدار ویژگی با شناسه {id} یافت نشد.")
{
}