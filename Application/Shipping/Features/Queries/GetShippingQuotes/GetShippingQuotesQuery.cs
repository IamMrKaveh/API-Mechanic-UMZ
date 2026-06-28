using Application.Shipping.Features.Shared;

namespace Application.Shipping.Features.Queries.GetShippingQuotes;

public sealed record GetShippingQuotesQuery(
    decimal OrderAmount,
    ICollection<ShippingQuoteItemDto> Items)
    : IQuery<IReadOnlyList<AvailableShippingDto>>;