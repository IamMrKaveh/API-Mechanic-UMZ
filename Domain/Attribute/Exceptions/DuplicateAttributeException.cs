using Domain.Common.Exceptions;

namespace Domain.Attribute.Exceptions;

public sealed class DuplicateAttributeException : DomainException
{
    public string Name { get; }

    public override string ErrorCode => "DUPLICATE_ATTRIBUTE";

    public DuplicateAttributeException(string name)
        : base($"ویژگی با نام '{name}' قبلاً وجود دارد.")
    {
        Name = name;
    }
}