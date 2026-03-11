using Domain.Inventory.Services.Results;

namespace Domain.Inventory.Services;

public sealed class InventoryReservationService : IInventoryReservationService
{
    public BatchReservationValidation ValidateBatchAvailability(IReadOnlyList<(int VariantId, int Quantity)> items)
    {
        var errors = new List<string>();

        foreach (var (variantId, quantity) in items)
        {
            if (quantity <= 0)
                errors.Add($"Variant {variantId}: quantity must be greater than zero.");
        }

        return errors.Count > 0
            ? BatchReservationValidation.Invalid(errors)
            : BatchReservationValidation.Valid();
    }

    public BatchReservationResult ReserveBatch(IReadOnlyList<(int VariantId, int Quantity)> items)
    {
        var validation = ValidateBatchAvailability(items);
        return validation.IsValid
            ? BatchReservationResult.Success()
            : BatchReservationResult.Fail(validation.Errors);
    }

    public BatchReleaseResult ReleaseBatch(IReadOnlyList<(int VariantId, int Quantity)> items)
    {
        if (items.Count == 0)
            return BatchReleaseResult.Fail("No items provided for release.");

        return BatchReleaseResult.Success();
    }
}