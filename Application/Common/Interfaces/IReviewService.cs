namespace Application.Common.Interfaces;

public interface IReviewService
{
    Task<ServiceResult<ProductReviewDto>> CreateReviewAsync(CreateReviewDto dto, int userId);
    Task<ServiceResult<PagedResultDto<ProductReviewDto>>> GetProductReviewsAsync(int productId, int page, int pageSize);
    Task<ServiceResult<IEnumerable<ProductReviewDto>>> GetUserReviewsAsync(int userId);
    Task<ServiceResult> UpdateReviewStatusAsync(int reviewId, string status);
    Task<ServiceResult> DeleteReviewAsync(int reviewId);
    Task<ServiceResult<PagedResultDto<ProductReviewDto>>> GetReviewsByStatusAsync(string status, int page, int pageSize);
}