namespace Application.Inventory.Features.Commands.AdjustStock;

public record AdjustStockCommand : IRequest<ServiceResult>
{
    public int VariantId { get; init; }
    public int QuantityChange { get; init; }
    public string Notes { get; init; } = string.Empty;
    public int UserId { get; init; }
}