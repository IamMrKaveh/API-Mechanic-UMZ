namespace Infrastructure.Payment.ZarinPal;

public enum ZarinPalErrorCode
{
    Success = 100,
    AlreadyVerified = 101,
    IncompleteInformation = -9,
    IpMismatch = -10,
    MerchantNotFound = -11,
    ShaparakThrottling = -12,
    AuthorityExpired = -22,
    InvalidAmount = -50,
    RequestNotFound = -51,
    TransactionError = -52,
    AuthorityMismatched = -53,
    ArchivedRequest = -54,
    OperationFailed = -1
}