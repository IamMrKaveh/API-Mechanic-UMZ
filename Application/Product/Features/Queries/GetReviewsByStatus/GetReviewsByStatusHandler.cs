namespace Application.Product.Features.Queries.GetReviewsByStatus;

public class GetReviewsByStatusHandler : IRequestHandler<GetReviewsByStatusQuery, ServiceResult<PaginatedResult<ProductReviewDto>>>
{
    private readonly IReviewQueryService _reviewQueryService;

    public GetReviewsByStatusHandler(IReviewQueryService reviewQueryService)
    {
        _reviewQueryService = reviewQueryService;
    }

    public async Task<ServiceResult<PaginatedResult<ProductReviewDto>>> Handle(GetReviewsByStatusQuery request, CancellationToken cancellationToken)
    {
        var result = await _reviewQueryService.GetReviewsByStatusAsync(request.Status, request.Page, request.PageSize, cancellationToken);

        return ServiceResult<PaginatedResult<ProductReviewDto>>.Success(result);
    }
}