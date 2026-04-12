using Application.Inventory.Features.Shared;

namespace Application.Inventory.Features.Queries.GetBatchVariantAvailability;

public record GetBatchVariantAvailabilityQuery(ICollection<Guid> VariantIds)
    : IRequest<ServiceResult<PaginatedResult<VariantAvailabilityDto>>>;