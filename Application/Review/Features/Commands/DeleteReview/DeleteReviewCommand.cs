using Application.Common.Results;

namespace Application.Review.Features.Commands.DeleteReview;

public record DeleteReviewCommand(Guid ReviewId) : IRequest<ServiceResult>;