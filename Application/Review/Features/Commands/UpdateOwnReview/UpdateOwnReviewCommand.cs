namespace Application.Review.Features.Commands.UpdateOwnReview;

public sealed record UpdateOwnReviewCommand(
    Guid ReviewId,
    int Rating,
    string? Title,
    string? Comment)
    : ICommand;