namespace Domain.Inventory.ValueObjects;

public enum StockEventType
{
    StockIn = 1,   // ورود موجودی
    Sale = 2,   // فروش
    Reservation = 3,   // رزرو
    ReservationRelease = 4,   // آزاد کردن رزرو
    ReservationCommit = 5,   // تأیید نهایی رزرو
    Adjustment = 6,   // تنظیم دستی
    Return = 7,   // برگشت کالا
    Damage = 8,   // ضایعات
    Transfer = 9,   // انتقال بین انبار
    InitialStock = 10,  // موجودی اولیه
}