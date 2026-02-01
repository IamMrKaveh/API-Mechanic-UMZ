using Application.Common.Interfaces.Product;
using Application.DTOs.Product;

namespace Application.Services.Product;

public class ReviewService : IReviewService
{
    private readonly IReviewRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHtmlSanitizer _htmlSanitizer;
    private readonly ILogger<ReviewService> _logger;
    private readonly IMapper _mapper;

    public ReviewService(IReviewRepository repository, IUnitOfWork unitOfWork, IHtmlSanitizer htmlSanitizer, ILogger<ReviewService> logger, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _htmlSanitizer = htmlSanitizer;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<ServiceResult<ProductReviewDto>> CreateReviewAsync(CreateReviewDto dto, int userId)
    {
        var hasPurchased = await _repository.HasUserPurchasedProductAsync(userId, dto.ProductId);

        var review = new ProductReview
        {
            ProductId = dto.ProductId,
            UserId = userId,
            Rating = dto.Rating,
            Title = !string.IsNullOrEmpty(dto.Title) ? _htmlSanitizer.Sanitize(dto.Title) : null,
            Comment = !string.IsNullOrEmpty(dto.Comment) ? _htmlSanitizer.Sanitize(dto.Comment) : null,
            CreatedAt = DateTime.UtcNow,
            IsVerifiedPurchase = hasPurchased,
            Status = "Pending"
        };

        await _repository.AddReviewAsync(review);
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Review created with ID {ReviewId} for product {ProductId} by user {UserId}", review.Id, review.ProductId, review.UserId);

        var resultDto = _mapper.Map<ProductReviewDto>(review);
        return ServiceResult<ProductReviewDto>.Ok(resultDto);
    }

    public async Task<ServiceResult<PagedResultDto<ProductReviewDto>>> GetProductReviewsAsync(int productId, int page, int pageSize)
    {
        var (reviews, totalCount) = await _repository.GetProductReviewsAsync(productId, page, pageSize);
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

    public async Task<ServiceResult<IEnumerable<ProductReviewDto>>> GetUserReviewsAsync(int userId)
    {
        var reviews = await _repository.GetUserReviewsAsync(userId);
        var dtos = _mapper.Map<IEnumerable<ProductReviewDto>>(reviews);
        return ServiceResult<IEnumerable<ProductReviewDto>>.Ok(dtos);
    }
}