using Application.Common.Results;
using Application.Inventory.Features.Queries.GetVariantAvailability;

namespace Application.Inventory.Features.Queries.GetBatchVariantAvailability;

public record GetBatchVariantAvailabilityQuery(IReadOnlyList<int> VariantIds)
    : IRequest<ServiceResult<IReadOnlyList<VariantAvailabilityDto>>>;