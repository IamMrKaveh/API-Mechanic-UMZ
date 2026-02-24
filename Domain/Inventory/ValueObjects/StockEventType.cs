namespace Domain.Inventory.ValueObjects;

public enum StockEventType
{
    StockIn = 1,   
    Sale = 2,   
    Reservation = 3,   
    ReservationRelease = 4,   
    ReservationCommit = 5,   
    Adjustment = 6,   
    Return = 7,   
    Damage = 8,   
    Transfer = 9,   
    InitialStock = 10,  
}