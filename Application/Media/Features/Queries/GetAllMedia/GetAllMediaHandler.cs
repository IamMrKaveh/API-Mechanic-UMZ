using Application.Media.Features.Shared;

namespace Application.Media.Features.Queries.GetAllMedia;

public class GetAllMediaHandler(IMediaQueryService mediaQueryService)
    : IRequestHandler<GetAllMediaQuery, ServiceResult<PaginatedResult<MediaDto>>>
{
    public async Task<ServiceResult<PaginatedResult<MediaDto>>> Handle(
        GetAllMediaQuery request,
        CancellationToken ct)
    {
        var result = await mediaQueryService.GetAllAsync(
            request.EntityType,
            request.Page,
            request.PageSize,
            ct);

        return ServiceResult<PaginatedResult<MediaDto>>.Success(result);
    }
}