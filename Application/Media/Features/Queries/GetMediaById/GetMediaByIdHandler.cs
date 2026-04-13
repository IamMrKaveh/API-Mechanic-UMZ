using Application.Media.Features.Shared;
using Domain.Media.ValueObjects;

namespace Application.Media.Features.Queries.GetMediaById;

public class GetMediaByIdHandler(IMediaQueryService mediaQueryService)
    : IRequestHandler<GetMediaByIdQuery, ServiceResult<MediaDto>>
{
    public async Task<ServiceResult<MediaDto>> Handle(
        GetMediaByIdQuery request,
        CancellationToken ct)
    {
        var mediaId = MediaId.From(request.MediaId);
        var result = await mediaQueryService.GetByIdAsync(mediaId, ct);

        if (result is null)
            return ServiceResult<MediaDto>.NotFound("رسانه یافت نشد.");

        return ServiceResult<MediaDto>.Success(result);
    }
}