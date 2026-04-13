using Application.Shipping.Features.Shared;

namespace Application.Variant.Features.Queries.GetVariantShipping;

public record GetVariantShippingQuery(Guid VariantId) : IRequest<ServiceResult<ProductVariantShippingInfoDto>>;