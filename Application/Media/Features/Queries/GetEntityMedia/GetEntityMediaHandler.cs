using Application.Media.Features.Shared;

namespace Application.Media.Features.Queries.GetEntityMedia;

public class GetEntityMediaHandler(IMediaQueryService mediaQueryService) : IRequestHandler<GetEntityMediaQuery, ServiceResult<IReadOnlyList<MediaDto>>>
{
    public async Task<ServiceResult<IReadOnlyList<MediaDto>>> Handle(
        GetEntityMediaQuery request, CancellationToken ct)
    {
        var result = await mediaQueryService.GetEntityMediaAsync(
            request.EntityType, request.EntityId, ct);

        return ServiceResult<IReadOnlyList<MediaDto>>.Success(result);
    }
}