namespace Domain.Attribute.Exceptions;

public sealed class AttributeValueNotFoundException(int id) : DomainException($"مقدار ویژگی با شناسه {id} یافت نشد.")
{
}