namespace Application.Media.Features.Queries.GetAllMedia;

public class GetAllMediaHandler
    : IRequestHandler<GetAllMediaQuery, ServiceResult<PaginatedResult<MediaListItemDto>>>
{
    private readonly IMediaQueryService _mediaQueryService;

    public GetAllMediaHandler(IMediaQueryService mediaQueryService)
    {
        _mediaQueryService = mediaQueryService;
    }

    public async Task<ServiceResult<PaginatedResult<MediaListItemDto>>> Handle(
        GetAllMediaQuery request, CancellationToken cancellationToken)
    {
        var result = await _mediaQueryService.GetAllMediaPagedAsync(
            request.EntityType,
            request.Page,
            request.PageSize,
            cancellationToken);

        return ServiceResult<PaginatedResult<MediaListItemDto>>.Success(result);
    }
}