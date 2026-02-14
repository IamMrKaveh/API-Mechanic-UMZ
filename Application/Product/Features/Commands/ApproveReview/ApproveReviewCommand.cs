namespace Application.Product.Features.Commands.ApproveReview;

public record ApproveReviewCommand(int ReviewId) : IRequest<ServiceResult>;