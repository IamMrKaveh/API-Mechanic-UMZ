using Application.Media.Contracts;
using Application.Media.Features.Shared;

namespace Application.Media.Features.Queries.GetEntityMedia;

public class GetEntityMediaHandler(IMediaQueryService mediaQueryService) : IRequestHandler<GetEntityMediaQuery, ServiceResult<IReadOnlyList<MediaDto>>>
{
    private readonly IMediaQueryService _mediaQueryService = mediaQueryService;

    public async Task<ServiceResult<IReadOnlyList<MediaDto>>> Handle(
        GetEntityMediaQuery request, CancellationToken cancellationToken)
    {
        var result = await _mediaQueryService.GetEntityMediaAsync(
            request.EntityType, request.EntityId, cancellationToken);

        return ServiceResult<IReadOnlyList<MediaDto>>.Success(result);
    }
}