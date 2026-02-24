namespace Domain.Wallet.ValueObjects;

public enum WalletTransactionType
{
    TopUp,
    OrderPayment,
    Refund,
    AdminAdjustmentCredit,
    AdminAdjustmentDebit,
    ReservationCommit
}