using Application.Common.Results;
using Domain.Review.ValueObjects;

namespace Application.Review.Features.Commands.ApproveReview;

public record ApproveReviewCommand(ProductReviewId ReviewId) : IRequest<ServiceResult>;