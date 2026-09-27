using Application.Media.Features.Shared;
using Domain.Media.ValueObjects;

namespace Application.Media.Features.Queries.GetMediaById;

public class GetMediaByIdHandler(IMediaQueryService mediaQueryService)
    : IQueryHandler<GetMediaByIdQuery, MediaDto>
{
    public async Task<ServiceResult<MediaDto>> Handle(
        GetMediaByIdQuery request,
        CancellationToken ct)
    {
        var mediaId = MediaId.From(request.MediaId);
        var resultResult = await (mediaQueryService.GetByIdAsync(mediaId, ct)).OrNotFoundAsync("رسانه یافت نشد.");
        if (resultResult.IsFailure) return resultResult.Error;
        var result = resultResult.Value;

        return ServiceResult<MediaDto>.Success(result);
    }
}