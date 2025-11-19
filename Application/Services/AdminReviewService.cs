namespace Application.Services;

public class AdminReviewService : IAdminReviewService
{
    private readonly IReviewRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdminReviewService> _logger;
    private readonly IMapper _mapper;

    public AdminReviewService(IReviewRepository repository, IUnitOfWork unitOfWork, ILogger<AdminReviewService> logger, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<ServiceResult> UpdateReviewStatusAsync(int reviewId, string status)
    {
        var review = await _repository.GetReviewByIdAsync(reviewId);
        if (review == null) return ServiceResult.Fail("Review not found");

        review.Status = status;
        review.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteReviewAsync(int reviewId)
    {
        var review = await _repository.GetReviewByIdAsync(reviewId);
        if (review == null) return ServiceResult.Fail("Review not found");

        review.IsDeleted = true;
        review.DeletedAt = DateTime.UtcNow;

        _repository.UpdateReview(review);
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Review with ID {ReviewId} deleted", reviewId);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<PagedResultDto<ProductReviewDto>>> GetReviewsByStatusAsync(string status, int page, int pageSize)
    {
        var (reviews, totalCount) = await _repository.GetReviewsByStatusAsync(status, page, pageSize);
        var dtos = _mapper.Map<List<ProductReviewDto>>(reviews);
        var pagedResult = new PagedResultDto<ProductReviewDto>
        {
            Items = dtos,
            TotalItems = totalCount,
            Page = page,
            PageSize = pageSize
        };
        return ServiceResult<PagedResultDto<ProductReviewDto>>.Ok(pagedResult);
    }
}