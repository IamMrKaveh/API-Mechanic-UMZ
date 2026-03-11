using Domain.Common.Abstractions;

namespace Domain.Product.ValueObjects;

public sealed record ProductId(Guid Value)
{
    public static ProductId NewId() => new(Guid.NewGuid());
    public static ProductId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}