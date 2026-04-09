namespace Application.Variant.Features.Queries.GetProductVariants;

public record GetProductVariantsQuery(int ProductId, bool ActiveOnly = true)
    : IRequest<ServiceResult<IEnumerable<ProductVariantViewDto>>>;