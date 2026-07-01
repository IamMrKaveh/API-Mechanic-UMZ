using Application.Variant.Features.Shared;

namespace Application.Variant.Features.Queries.GetVariantShipping;

public record GetVariantShippingQuery(
    Guid VariantId)
    : IQuery<VariantShippingInfoDto>;