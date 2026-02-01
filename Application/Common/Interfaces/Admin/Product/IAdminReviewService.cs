using Application.DTOs.Product;

namespace Application.Common.Interfaces.Admin.Product;

public interface IAdminReviewService
{
    Task<ServiceResult> UpdateReviewStatusAsync(int reviewId, string status);
    Task<ServiceResult> DeleteReviewAsync(int reviewId);
    Task<ServiceResult<PagedResultDto<ProductReviewDto>>> GetReviewsByStatusAsync(string status, int page, int pageSize);
}