namespace Application.Media.Features.Queries.GetMediaById;

public class GetMediaByIdHandler(IMediaQueryService mediaQueryService)
        : IRequestHandler<GetMediaByIdQuery, ServiceResult<MediaDetailDto?>>
{
    private readonly IMediaQueryService _mediaQueryService = mediaQueryService;

    public async Task<ServiceResult<MediaDetailDto?>> Handle(
        GetMediaByIdQuery request,
        CancellationToken ct)
    {
        var result = await _mediaQueryService.GetMediaByIdAsync(
            request.MediaId, ct);

        if (result is null)
            return ServiceResult<MediaDetailDto?>.NotFound("رسانه یافت نشد.");

        return ServiceResult<MediaDetailDto?>.Success(result);
    }
}