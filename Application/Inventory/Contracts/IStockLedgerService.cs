using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Application.Inventory.Contracts;

public interface IStockLedgerService
{
    Task ReconcileAsync(
        VariantId variantId,
        UserId userId,
        CancellationToken ct = default);
}