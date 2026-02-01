namespace Application.Services.Admin.Product;

public class AdminReviewService : IAdminReviewService
{
    private readonly IReviewRepository _reviewRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppLogger<AdminReviewService> _logger;

    public AdminReviewService(
        IReviewRepository reviewRepository,
        IUnitOfWork unitOfWork,
        IAppLogger<AdminReviewService> logger)
    {
        _reviewRepository = reviewRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult> UpdateReviewStatusAsync(int reviewId, string status)
    {
        var review = await _reviewRepository.GetByIdAsync(reviewId);
        if (review == null)
        {
            return ServiceResult.Fail("Review not found");
        }

        review.Status = status;
        _reviewRepository.Update(review);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Review {ReviewId} status updated to {Status}", reviewId, status);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteReviewAsync(int reviewId)
    {
        var review = await _reviewRepository.GetByIdAsync(reviewId);
        if (review == null)
        {
            return ServiceResult.Fail("Review not found");
        }

        review.IsDeleted = true;
        review.DeletedAt = DateTime.UtcNow;
        _reviewRepository.Update(review);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Review {ReviewId} soft deleted", reviewId);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<PagedResultDto<ProductReviewDto>>> GetReviewsByStatusAsync(string status, int page, int pageSize)
    {
        var (reviews, totalItems) = await _reviewRepository.GetReviewsByStatusAsync(status, page, pageSize);

        var reviewDtos = reviews.Select(r => new ProductReviewDto
        {
            Id = r.Id,
            ProductId = r.ProductId,
            UserId = r.UserId,
            UserName = r.User != null ? $"{r.User.FirstName} {r.User.LastName}" : "Unknown",
            Rating = r.Rating,
            Title = r.Title,
            Comment = r.Comment,
            Status = r.Status,
            IsVerifiedPurchase = r.IsVerifiedPurchase,
            CreatedAt = r.CreatedAt
        }).ToList();

        var result = PagedResultDto<ProductReviewDto>.Create(reviewDtos, totalItems, page, pageSize);
        return ServiceResult<PagedResultDto<ProductReviewDto>>.Ok(result);
    }
}