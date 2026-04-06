using Application.Common.Results;
using Application.Review.Features.Shared;

namespace Application.Review.Features.Commands.CreateReview;

public record CreateReviewCommand(
    Guid ProductId,
    Guid UserId,
    Guid? OrderId,
    int Rating,
    string? Title,
    string? Comment) : IRequest<ServiceResult<ProductReviewDto>>;