using Application.Shipping.Features.Shared;

namespace Application.Shipping.Features.Queries.GetAvailableShippingsForVariants;

public sealed record GetAvailableShippingsForVariantsQuery(
    List<Guid> VariantIds) : IRequest<ServiceResult<IReadOnlyList<AvailableShippingDto>>>;