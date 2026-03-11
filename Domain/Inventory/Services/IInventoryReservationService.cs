namespace Domain.Inventory.Services;

public interface IInventoryReservationService
{
    BatchReservationValidation ValidateBatchAvailability(IReadOnlyList<(int VariantId, int Quantity)> items);

    BatchReservationResult ReserveBatch(IReadOnlyList<(int VariantId, int Quantity)> items);

    BatchReleaseResult ReleaseBatch(IReadOnlyList<(int VariantId, int Quantity)> items);
}