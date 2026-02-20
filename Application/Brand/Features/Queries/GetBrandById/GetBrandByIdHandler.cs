namespace Application.Brand.Features.Queries.GetBrandById;

public class GetBrandByIdHandler
    : IRequestHandler<GetBrandByIdQuery, ServiceResult<BrandDetailDto?>>
{
    private readonly ICategoryQueryService _queryService;

    public GetBrandByIdHandler(ICategoryQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<ServiceResult<BrandDetailDto?>> Handle(
        GetBrandByIdQuery request, CancellationToken cancellationToken)
    {
        var result = await _queryService.GetBrandDetailAsync(request.Id, cancellationToken);

        if (result == null)
            return ServiceResult<BrandDetailDto?>.Failure("گروه یافت نشد.", 404);

        return ServiceResult<BrandDetailDto?>.Success(result);
    }
}