using Application.Common.Models;

namespace Application.Inventory.Features.Queries.GetBatchVariantAvailability;

public record GetBatchVariantAvailabilityQuery(IReadOnlyList<int> VariantIds)
    : IRequest<ServiceResult<IReadOnlyList<VariantAvailabilityDto>>>;