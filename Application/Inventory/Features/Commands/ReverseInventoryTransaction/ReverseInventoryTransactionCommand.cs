using Application.Common.Results;

namespace Application.Inventory.Features.Commands.ReverseInventoryTransaction;

public record ReverseInventoryTransactionCommand : IRequest<ServiceResult>
{
    public int TransactionId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public int AdminUserId { get; init; }
}