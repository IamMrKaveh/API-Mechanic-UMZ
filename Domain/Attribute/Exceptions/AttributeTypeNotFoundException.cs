namespace Domain.Attribute.Exceptions;

public sealed class AttributeTypeNotFoundException(int id) : DomainException($"ویژگی با شناسه {id} یافت نشد.")
{
}