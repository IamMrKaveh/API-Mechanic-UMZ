using Application.Common.Results;
using Application.Review.Features.Shared;

namespace Application.Review.Features.Commands.SubmitReview;

public record SubmitReviewCommand(
    Guid ProductId,
    Guid UserId,
    Guid? OrderId,
    int Rating,
    string? Title,
    string? Comment) : IRequest<ServiceResult<ProductReviewDto>>;