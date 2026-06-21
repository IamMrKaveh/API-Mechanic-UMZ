using Application.Inventory.Features.Shared;

namespace Application.Inventory.Features.Queries.GetVariantAvailability;

public record GetVariantAvailabilityQuery(Guid VariantId)
    : IQuery<VariantAvailabilityDto>;