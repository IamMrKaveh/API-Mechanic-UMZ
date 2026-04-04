using Application.Common.Results;
using Domain.Review.ValueObjects;

namespace Application.Review.Features.Commands.ReplyToReview;

public record ReplyToReviewCommand(ProductReviewId ReviewId, string Reply) : IRequest<ServiceResult>;