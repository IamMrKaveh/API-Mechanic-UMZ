using Application.Shipping.Features.Shared;

namespace Application.Shipping.Features.Queries.GetShipping;

public record GetShippingQuery(
    Guid Id)
    : IQuery<ShippingDto>;