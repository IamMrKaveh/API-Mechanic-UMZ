using Domain.Common.Exceptions;

namespace Domain.Attribute.Exceptions;

public sealed class DuplicateAttributeException(string name) : DomainException($"ویژگی با نام '{name}' قبلاً وجود دارد.")
{
    public string Name { get; } = name;
}