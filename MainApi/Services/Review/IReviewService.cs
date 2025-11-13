namespace MainApi.Services.Review;

public interface IReviewService
{
    Task<ProductReviewDto> CreateReviewAsync(CreateReviewDto dto, int userId);
    Task<IEnumerable<ProductReviewDto>> GetProductReviewsAsync(int productId, int page, int pageSize);
    Task<IEnumerable<ProductReviewDto>> GetUserReviewsAsync(int userId);
    Task<bool> UpdateReviewStatusAsync(int reviewId, string status);
    Task<bool> DeleteReviewAsync(int reviewId);
}