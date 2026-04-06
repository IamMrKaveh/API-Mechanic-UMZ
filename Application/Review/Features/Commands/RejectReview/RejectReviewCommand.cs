using Application.Common.Results;

namespace Application.Review.Features.Commands.RejectReview;

public record RejectReviewCommand(Guid ReviewId, string? Reason) : IRequest<ServiceResult>;