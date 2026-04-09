using Application.Inventory.Features.Queries.GetVariantAvailability;

namespace Application.Inventory.Features.Queries.GetBatchVariantAvailability;

public record GetBatchVariantAvailabilityQuery(IReadOnlyList<Guid> VariantIds)
    : IRequest<ServiceResult<PaginatedResult<VariantAvailabilityDto>>>;