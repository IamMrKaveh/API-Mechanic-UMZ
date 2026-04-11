namespace Application.Media.Features.Queries.GetAllMedia;

public class GetAllMediaHandler(IMediaQueryService mediaQueryService)
        : IRequestHandler<GetAllMediaQuery, ServiceResult<PaginatedResult<MediaListItemDto>>>
{
    public async Task<ServiceResult<PaginatedResult<MediaListItemDto>>> Handle(
        GetAllMediaQuery request, CancellationToken ct)
    {
        var result = await mediaQueryService.GetAllMediaPagedAsync(
            request.EntityType,
            request.Page,
            request.PageSize,
            ct);

        return ServiceResult<PaginatedResult<MediaListItemDto>>.Success(result);
    }
}