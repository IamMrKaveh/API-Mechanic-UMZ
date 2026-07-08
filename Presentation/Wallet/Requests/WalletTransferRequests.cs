namespace Presentation.Wallet.Requests;

public sealed record PreviewWalletTransferRequest(
    string RecipientPhoneNumber,
    decimal Amount);

public sealed record InitiateWalletTransferRequest(
    string RecipientPhoneNumber,
    decimal Amount,
    string? Description);

public sealed record ConfirmWalletTransferRequest(
    Guid TransferId,
    string OtpCode);

public sealed record CancelWalletTransferRequest(Guid TransferId);