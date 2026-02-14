namespace Application.Product.Features.Commands.RejectReview;

public record RejectReviewCommand(int ReviewId, string? Reason) : IRequest<ServiceResult>;