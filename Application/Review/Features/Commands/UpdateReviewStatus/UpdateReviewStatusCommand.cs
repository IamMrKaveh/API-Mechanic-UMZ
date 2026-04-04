using Application.Common.Results;
using Domain.Review.ValueObjects;

namespace Application.Review.Features.Commands.UpdateReviewStatus;

public record UpdateReviewStatusCommand(ProductReviewId ReviewId, string Status) : IRequest<ServiceResult>;