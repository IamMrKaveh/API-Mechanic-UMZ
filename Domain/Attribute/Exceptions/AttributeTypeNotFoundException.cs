namespace Domain.Attribute.Exceptions;

public sealed class AttributeTypeNotFoundException : DomainException
{
    public AttributeTypeNotFoundException(int id)
        : base($"ویژگی با شناسه {id} یافت نشد.") { }
}