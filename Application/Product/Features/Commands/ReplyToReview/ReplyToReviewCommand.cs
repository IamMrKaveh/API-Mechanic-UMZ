namespace Application.Product.Features.Commands.ReplyToReview;

public record ReplyToReviewCommand(int ReviewId, string Reply) : IRequest<ServiceResult>;