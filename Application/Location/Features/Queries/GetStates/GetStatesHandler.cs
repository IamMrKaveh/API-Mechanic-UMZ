using Application.Location.Contracts;
using Application.Location.Features.Queries.GetStates;
using Application.Location.Features.Shared;

public class GetStatesHandler(ILocationService locationService)
    : IQueryHandler<GetStatesQuery, PaginatedResult<ProvinceDto>>
{
    public async Task<ServiceResult<PaginatedResult<ProvinceDto>>> Handle(
        GetStatesQuery request,
        CancellationToken ct)
    {
        var provinces = await locationService.GetProvincesAsync(ct);
        var list = provinces.ToList();

        var pageSize = list.Count > 0 ? list.Count : Math.Max(1, request.PageSize);
        var result = PaginatedResult<ProvinceDto>.Create(list, list.Count, 1, pageSize);

        return ServiceResult<PaginatedResult<ProvinceDto>>.Success(result);
    }
}
