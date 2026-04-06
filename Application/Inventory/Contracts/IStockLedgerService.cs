namespace Application.Inventory.Contracts;

public interface IStockLedgerService
{
    Task ReconcileAsync(Guid variantId, Guid userId, CancellationToken ct = default);
}