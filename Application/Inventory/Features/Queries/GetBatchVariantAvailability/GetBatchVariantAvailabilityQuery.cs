using Application.Inventory.Features.Shared;

namespace Application.Inventory.Features.Queries.GetBatchVariantAvailability;

public record GetBatchVariantAvailabilityQuery(IReadOnlyList<Guid> VariantIds)
    : IRequest<ServiceResult<PaginatedResult<VariantAvailabilityDto>>>;