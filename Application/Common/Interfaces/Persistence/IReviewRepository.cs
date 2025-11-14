namespace Application.Common.Interfaces.Persistence;

public interface IReviewRepository
{
    Task<bool> HasUserPurchasedProductAsync(int userId, int productId);
    Task AddReviewAsync(Domain.Product.ProductReview review);
    Task<Domain.Product.ProductReview?> GetReviewByIdAsync(int reviewId);
    void DeleteReview(Domain.Product.ProductReview review);
    Task<(List<Domain.Product.ProductReview> Reviews, int TotalCount)> GetProductReviewsAsync(int productId, int page, int pageSize);
    Task<IEnumerable<Domain.Product.ProductReview>> GetUserReviewsAsync(int userId);
    Task<(List<Domain.Product.ProductReview> Reviews, int TotalCount)> GetReviewsByStatusAsync(string status, int page, int pageSize);
}