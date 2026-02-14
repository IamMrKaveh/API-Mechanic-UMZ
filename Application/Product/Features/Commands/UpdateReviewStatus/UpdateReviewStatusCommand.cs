namespace Application.Product.Features.Commands.UpdateReviewStatus;

public record UpdateReviewStatusCommand(int ReviewId, string Status) : IRequest<ServiceResult>;