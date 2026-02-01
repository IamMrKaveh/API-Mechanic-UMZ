using Application.DTOs.Product;

namespace Application.Common.Interfaces.Product;

public interface IReviewService
{
    Task<ServiceResult<ProductReviewDto>> CreateReviewAsync(CreateReviewDto dto, int userId);
    Task<ServiceResult<PagedResultDto<ProductReviewDto>>> GetProductReviewsAsync(int productId, int page, int pageSize);
    Task<ServiceResult<IEnumerable<ProductReviewDto>>> GetUserReviewsAsync(int userId);
}