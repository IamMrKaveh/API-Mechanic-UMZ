namespace Presentation.Wallet.Requests;

public sealed record FraudAlertReviewRequest(string? Note);

public sealed record FraudAlertDismissRequest(string? Note);

public sealed record GetFraudAlertsRequest(
    string? Status = null,
    string? Severity = null,
    Guid? UserId = null,
    int Page = 1,
    int PageSize = 20);