using Domain.Inventory.Services.Results;
using Domain.Inventory.ValueObjects;

namespace Domain.Inventory.Services;

public interface IInventoryReservationService
{
    BatchReservationValidation ValidateBatchAvailability(
        IReadOnlyList<BatchReservationItem> items);

    BatchReservationResult ReserveBatch(
        IReadOnlyList<BatchReservationItem> items);

    BatchReleaseResult ReleaseBatch(
        IReadOnlyList<BatchReservationItem> items);
}