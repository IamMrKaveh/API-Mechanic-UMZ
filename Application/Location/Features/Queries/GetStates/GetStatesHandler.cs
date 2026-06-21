using Application.Location.Contracts;
using Application.Location.Features.Shared;

namespace Application.Location.Features.Queries.GetStates;

public class GetStatesHandler(ILocationService locationService)
    : IQueryHandler<GetStatesQuery, PaginatedResult<ProvinceDto>>
{
    public async Task<ServiceResult<PaginatedResult<ProvinceDto>>> Handle(
        GetStatesQuery request,
        CancellationToken ct)
    {
        var provinces = await locationService.GetProvincesAsync(ct);
        var list = provinces.ToList();
        var result = PaginatedResult<ProvinceDto>.Create(list, list.Count, 1, list.Count);
        return ServiceResult<PaginatedResult<ProvinceDto>>.Success(result);
    }
}