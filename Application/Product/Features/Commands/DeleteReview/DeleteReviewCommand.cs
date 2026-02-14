namespace Application.Product.Features.Commands.DeleteReview;

public record DeleteReviewCommand(int ReviewId) : IRequest<ServiceResult>;