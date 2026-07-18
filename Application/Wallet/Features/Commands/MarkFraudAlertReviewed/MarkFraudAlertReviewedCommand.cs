namespace Application.Wallet.Features.Commands.MarkFraudAlertReviewed;

public sealed record MarkFraudAlertReviewedCommand(
    Guid AlertId,
    string? Note) : ICommand<Unit>;