namespace Application.Review.Features.Commands.UpdateReviewStatus;

public record UpdateReviewStatusCommand(Guid ReviewId, string Status) : IRequest<ServiceResult>;