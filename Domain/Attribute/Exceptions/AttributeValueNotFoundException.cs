namespace Domain.Attribute.Exceptions;

public sealed class AttributeValueNotFoundException : DomainException
{
    public AttributeValueNotFoundException(int id)
        : base($"مقدار ویژگی با شناسه {id} یافت نشد.") { }
}