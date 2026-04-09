using Application.Common.Results;
using Application.Shipping.Features.Shared;

namespace Application.Variant.Features.Queries.GetProductVariantShipping;

public record GetVariantShippingQuery(Guid VariantId) : IRequest<ServiceResult<ProductVariantShippingInfoDto>>;