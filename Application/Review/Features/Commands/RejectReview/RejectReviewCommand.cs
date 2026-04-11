namespace Application.Review.Features.Commands.RejectReview;

public record RejectReviewCommand(
    Guid ReviewId,
    string Reason,
    Guid UserId) : IRequest<ServiceResult>;