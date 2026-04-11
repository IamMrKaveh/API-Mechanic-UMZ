namespace Presentation.Review.Requests;

public record CreateReviewRequest(
    Guid ProductId,
    Guid? OrderId,
    int Rating,
    string? Title,
    string? Comment
);