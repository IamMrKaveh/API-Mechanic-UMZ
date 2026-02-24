namespace Application.Review.Features.Commands.ApproveReview;

public record ApproveReviewCommand(int ReviewId) : IRequest<ServiceResult>;