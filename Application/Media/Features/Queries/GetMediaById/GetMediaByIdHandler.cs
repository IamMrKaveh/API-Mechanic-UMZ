namespace Application.Media.Features.Queries.GetMediaById;

public class GetMediaByIdHandler(IMediaQueryService mediaQueryService)
        : IRequestHandler<GetMediaByIdQuery, ServiceResult<MediaDetailDto?>>
{
    public async Task<ServiceResult<MediaDetailDto?>> Handle(
        GetMediaByIdQuery request,
        CancellationToken ct)
    {
        var result = await mediaQueryService.GetMediaByIdAsync(
            request.MediaId, ct);

        if (result is null)
            return ServiceResult<MediaDetailDto?>.NotFound("رسانه یافت نشد.");

        return ServiceResult<MediaDetailDto?>.Success(result);
    }
}