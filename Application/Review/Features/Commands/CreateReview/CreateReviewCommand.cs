using Application.Review.Features.Shared;

namespace Application.Review.Features.Commands.CreateReview;

public record CreateReviewCommand(
    Guid ProductId,
    Guid? OrderId,
    int Rating,
    string? Title,
    string? Comment)
    : ICommand<ProductReviewDto>, IHasUniqueConstraintMapping
{
    public string? MapUniqueConstraintViolation(string? constraintName)
    {
        if (string.IsNullOrEmpty(constraintName))
            return "شما قبلاً برای این محصول نظر ثبت کرده‌اید.";

        if (constraintName.Contains("ProductReviews", StringComparison.OrdinalIgnoreCase)
            && constraintName.Contains("UserId", StringComparison.OrdinalIgnoreCase))
            return "شما قبلاً برای این محصول نظر ثبت کرده‌اید.";

        return "این رکورد از قبل وجود دارد.";
    }
}
