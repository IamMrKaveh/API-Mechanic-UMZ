namespace Application.Review.Features.Commands.DeleteOwnReview;

public sealed record DeleteOwnReviewCommand(
    Guid ReviewId)
    : ICommand;