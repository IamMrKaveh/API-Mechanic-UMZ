using Application.Common.Models;

namespace Application.Review.Features.Commands.UpdateReviewStatus;

public record UpdateReviewStatusCommand(int ReviewId, string Status) : IRequest<ServiceResult>;