namespace Application.Review.Features.Commands.DeleteReview;

public record DeleteReviewCommand(int ReviewId) : IRequest<ServiceResult>;