namespace Application.Inventory.Features.Commands.RecordDamage;

public record RecordDamageCommand : IRequest<ServiceResult>
{
    public int VariantId { get; init; }
    public int Quantity { get; init; }
    public string Notes { get; init; } = string.Empty;
    public int UserId { get; init; }
}