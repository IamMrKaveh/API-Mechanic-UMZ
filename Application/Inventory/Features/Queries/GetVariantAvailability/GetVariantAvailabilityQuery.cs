namespace Application.Inventory.Features.Queries.GetVariantAvailability;

public record GetVariantAvailabilityQuery(Guid VariantId) : IRequest<ServiceResult<VariantAvailabilityDto>>;

public class VariantAvailabilityDto
{
    public Guid VariantId { get; set; }
    public int OnHand { get; set; }
    public int Reserved { get; set; }
    public int Available { get; set; }
    public bool IsInStock { get; set; }
    public bool IsUnlimited { get; set; }
    public DateTime LastUpdated { get; set; }
}