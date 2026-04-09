using Application.Variant.Features.Shared;

namespace Application.Variant.Features.Queries.GetProductVariants;

public record GetProductVariantsQuery(Guid ProductId, bool ActiveOnly = true)
    : IRequest<ServiceResult<PaginatedResult<ProductVariantViewDto>>>;