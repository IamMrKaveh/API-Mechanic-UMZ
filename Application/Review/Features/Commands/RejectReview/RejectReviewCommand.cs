using Application.Common.Results;
using Domain.Review.ValueObjects;

namespace Application.Review.Features.Commands.RejectReview;

public record RejectReviewCommand(ProductReviewId ReviewId, string? Reason) : IRequest<ServiceResult>;