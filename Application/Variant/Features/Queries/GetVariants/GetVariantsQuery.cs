using Application.Variant.Features.Shared;

namespace Application.Variant.Features.Queries.GetVariants;

public record GetVariantsQuery(Guid ProductId, bool ActiveOnly = true)
    : IRequest<ServiceResult<IEnumerable<ProductVariantViewDto>>>;