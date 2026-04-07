using Application.Common.Results;
using Domain.Review.ValueObjects;

namespace Application.Review.Features.Commands.UpdateReviewStatus;

public record UpdateReviewStatusCommand(ReviewId ReviewId, string Status) : IRequest<ServiceResult>;