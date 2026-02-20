namespace Application.Brand.Features.Queries.GetBrandDetail;

public class GetBrandDetailHandler
    : IRequestHandler<GetBrandDetailQuery, ServiceResult<BrandDetailDto?>>
{
    private readonly ICategoryQueryService _queryService;

    public GetBrandDetailHandler(ICategoryQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<ServiceResult<BrandDetailDto?>> Handle(
        GetBrandDetailQuery request, CancellationToken cancellationToken)
    {
        var result = await _queryService.GetBrandDetailAsync(
            request.GroupId, cancellationToken);

        if (result == null)
            return ServiceResult<BrandDetailDto?>.Failure("گروه یافت نشد.", 404);

        return ServiceResult<BrandDetailDto?>.Success(result);
    }
}