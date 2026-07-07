namespace Application.Wallet.Features.Commands.MarkFraudAlertReviewed;

public sealed record MarkFraudAlertReviewedCommand(
    Guid AlertId,
    Guid AdminId,
    string? Note) : ICommand<Unit>;