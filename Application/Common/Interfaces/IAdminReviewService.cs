namespace Application.Common.Interfaces;

public interface IAdminReviewService
{
    Task<ServiceResult> UpdateReviewStatusAsync(int reviewId, string status);
    Task<ServiceResult> DeleteReviewAsync(int reviewId);
    Task<ServiceResult<PagedResultDto<ProductReviewDto>>> GetReviewsByStatusAsync(string status, int page, int pageSize);
}