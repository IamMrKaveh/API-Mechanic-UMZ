using Application.Shipping.Features.Shared;

namespace Application.Shipping.Features.Queries.GetShippings;

public record GetShippingsQuery(
    bool IncludeInactive = false)
    : IQuery<IReadOnlyList<ShippingListItemDto>>;