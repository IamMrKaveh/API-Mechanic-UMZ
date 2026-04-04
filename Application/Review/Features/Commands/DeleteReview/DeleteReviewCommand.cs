using Application.Common.Results;
using Domain.Review.ValueObjects;

namespace Application.Review.Features.Commands.DeleteReview;

public record DeleteReviewCommand(ProductReviewId ReviewId) : IRequest<ServiceResult>;