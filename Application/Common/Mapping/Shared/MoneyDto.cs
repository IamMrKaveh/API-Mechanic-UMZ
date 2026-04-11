namespace Application.Common.Mapping.Shared;

public sealed record MoneyDto(decimal Amount, string Currency);

public sealed record PercentageDto(decimal Value);