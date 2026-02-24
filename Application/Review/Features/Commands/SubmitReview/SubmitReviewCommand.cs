namespace Application.Review.Features.Commands.SubmitReview;

public record SubmitReviewCommand : IRequest<ServiceResult<ProductReviewDto>>
{
    public int ProductId { get; init; }
    public int UserId { get; init; }
    public int? OrderId { get; init; }
    public int Rating { get; init; }
    public string? Title { get; init; }
    public string? Comment { get; init; }
}