using Domain.Common.Exceptions;

namespace Domain.Shipping.Exceptions;

public sealed class DuplicateShippingNameException : DomainException
{
    public string Name { get; }

    public override string ErrorCode => "DUPLICATE_SHIPPING_NAME";

    public DuplicateShippingNameException(string name)
        : base($"روش ارسال با نام '{name}' قبلاً وجود دارد.")
    {
        Name = name;
    }
}