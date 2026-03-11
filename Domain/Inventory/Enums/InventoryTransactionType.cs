namespace Domain.Inventory.Enums;

public enum InventoryTransactionType
{
    StockIn = 1,
    StockOut = 2,
    Reservation = 3,
    ReservationRelease = 4,
    Adjustment = 5
}