using Application.Review.Features.Shared;

namespace Application.Review.Features.Commands.CreateReview;

public record CreateReviewCommand(
    Guid UserId,
    Guid ProductId,
    Guid? OrderId,
    int Rating,
    string? Title,
    string? Comment) : IRequest<ServiceResult<ProductReviewDto>>;