namespace Application.Product.Features.Queries.GetProductVariantShippingMethods;

public record GetProductVariantShippingMethodsQuery(int VariantId) : IRequest<ServiceResult<ProductVariantShippingInfoDto>>;