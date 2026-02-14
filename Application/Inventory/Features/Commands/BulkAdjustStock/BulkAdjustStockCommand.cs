using Application.Inventory.Contracts;

namespace Application.Inventory.Features.Commands.BulkAdjustStock;

public record BulkAdjustStockCommand : IRequest<ServiceResult<BulkAdjustResultDto>>
{
    public List<BulkAdjustItemDto> Items { get; init; } = [];
    public int UserId { get; init; }
}