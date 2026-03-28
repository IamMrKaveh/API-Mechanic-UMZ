using Domain.Inventory.Services.Results;
using Domain.Inventory.ValueObjects;

namespace Domain.Inventory.Services;

public sealed class InventoryReservationService : IInventoryReservationService
{
    public BatchReservationValidation ValidateBatchAvailability(IReadOnlyList<BatchReservationItem> items)
    {
        var errors = new List<string>();

        foreach (var item in items)
        {
            if (item.Quantity <= 0)
                errors.Add($"Variant {item.VariantId}: quantity must be greater than zero.");
        }

        return errors.Count > 0
            ? BatchReservationValidation.Invalid(errors)
            : BatchReservationValidation.Valid();
    }

    public BatchReservationResult ReserveBatch(IReadOnlyList<BatchReservationItem> items)
    {
        var validation = ValidateBatchAvailability(items);
        return validation.IsValid
            ? BatchReservationResult.Success()
            : BatchReservationResult.Fail(validation.Errors);
    }

    public BatchReleaseResult ReleaseBatch(IReadOnlyList<BatchReservationItem> items)
    {
        if (items.Count == 0)
            return BatchReleaseResult.Fail("No items provided for release.");

        return BatchReleaseResult.Success();
    }
}