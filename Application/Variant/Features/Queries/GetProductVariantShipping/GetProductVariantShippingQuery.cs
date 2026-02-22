namespace Application.Variant.Features.Queries.GetProductVariantShipping;

public record GetProductVariantShippingQuery(int VariantId) : IRequest<ServiceResult<ProductVariantShippingInfoDto>>;