namespace Application.Media.Features.Queries.GetMediaById;

public class GetMediaByIdHandler
    : IRequestHandler<GetMediaByIdQuery, ServiceResult<MediaDetailDto?>>
{
    private readonly IMediaQueryService _mediaQueryService;

    public GetMediaByIdHandler(IMediaQueryService mediaQueryService)
    {
        _mediaQueryService = mediaQueryService;
    }

    public async Task<ServiceResult<MediaDetailDto?>> Handle(
        GetMediaByIdQuery request, CancellationToken cancellationToken)
    {
        var result = await _mediaQueryService.GetMediaByIdAsync(
            request.MediaId, cancellationToken);

        if (result == null)
            return ServiceResult<MediaDetailDto?>.Failure("رسانه یافت نشد.", 404);

        return ServiceResult<MediaDetailDto?>.Success(result);
    }
}