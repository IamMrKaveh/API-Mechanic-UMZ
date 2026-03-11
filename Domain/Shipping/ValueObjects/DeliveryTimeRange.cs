namespace Domain.Shipping.ValueObjects;

public sealed class DeliveryTimeRange : ValueObject
{
    public int MinDays { get; }
    public int MaxDays { get; }

    private const int AbsoluteMaxDays = 365;

    private DeliveryTimeRange(int minDays, int maxDays)
    {
        MinDays = minDays;
        MaxDays = maxDays;
    }

    public static DeliveryTimeRange Create(int minDays, int maxDays)
    {
        if (minDays < 0)
            throw new DomainException("حداقل روز تحویل نمی‌تواند منفی باشد.");

        if (maxDays < minDays)
            throw new DomainException("حداکثر روز تحویل نمی‌تواند کمتر از حداقل باشد.");

        if (maxDays > AbsoluteMaxDays)
            throw new DomainException($"حداکثر روز تحویل نمی‌تواند بیش از {AbsoluteMaxDays} روز باشد.");

        return new DeliveryTimeRange(minDays, maxDays);
    }

    public static DeliveryTimeRange Default() => new(1, 7);

    public bool IsSameDay => MinDays == MaxDays;

    public string ToDisplayString()
    {
        if (IsSameDay)
            return $"{MinDays} روز کاری";

        return $"{MinDays} تا {MaxDays} روز کاری";
    }

    public string ToDisplayString(string? customLabel)
    {
        if (!string.IsNullOrWhiteSpace(customLabel))
            return customLabel;

        return ToDisplayString();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return MinDays;
        yield return MaxDays;
    }

    public override string ToString() => ToDisplayString();
}