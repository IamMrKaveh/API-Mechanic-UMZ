using Application.Common.Results;

namespace Application.Review.Features.Commands.ApproveReview;

public record ApproveReviewCommand(Guid ReviewId) : IRequest<ServiceResult>;