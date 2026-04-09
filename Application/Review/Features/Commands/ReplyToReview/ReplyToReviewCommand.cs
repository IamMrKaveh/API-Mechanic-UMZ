using Application.Common.Results;

namespace Application.Review.Features.Commands.ReplyToReview;

public record ReplyToReviewCommand(Guid ReviewId, string Reply) : IRequest<ServiceResult>;