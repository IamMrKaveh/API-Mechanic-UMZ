using Domain.Attribute.ValueObjects;

namespace Domain.Attribute.Exceptions;

public sealed class AttributeTypeNotFoundException(AttributeTypeId id) : DomainException($"ویژگی با شناسه {id.Value} یافت نشد.")
{
}