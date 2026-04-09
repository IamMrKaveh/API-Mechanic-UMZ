using Application.Inventory.Features.Queries.GetVariantAvailability;

namespace Application.Inventory.Features.Queries.GetBatchVariantAvailability;

public record GetBatchVariantAvailabilityQuery(ICollection<Guid> VariantIds)
    : IRequest<ServiceResult<PaginatedResult<VariantAvailabilityDto>>>;