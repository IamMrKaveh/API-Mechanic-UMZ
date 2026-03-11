namespace Domain.Shipping.Exceptions;

public sealed class DuplicateShippingNameException(string name)
    : DomainException($"روش ارسال با نام '{name}' قبلاً وجود دارد.")
{
    public string Name { get; } = name;
}