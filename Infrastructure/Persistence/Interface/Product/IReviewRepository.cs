namespace Infrastructure.Persistence.Interface.Product;

public interface IReviewRepository
{
    Task<bool> HasUserPurchasedProductAsync(int userId, int productId);

    Task AddReviewAsync(ProductReview review);

    void UpdateReview(ProductReview review);

    Task<ProductReview?> GetReviewByIdAsync(int reviewId);

    void DeleteReview(ProductReview review);

    Task<(List<ProductReview> Reviews, int TotalCount)> GetProductReviewsAsync(int productId, int page, int pageSize);

    Task<IEnumerable<ProductReview>> GetUserReviewsAsync(int userId);

    Task<(List<ProductReview> Reviews, int TotalCount)> GetReviewsByStatusAsync(string status, int page, int pageSize);
}