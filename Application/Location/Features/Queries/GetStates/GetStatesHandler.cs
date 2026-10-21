using Application.Common.Validation;
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

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, PaginationDefaults.MaxPageSize);

        var items = list
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var result = PaginatedResult<ProvinceDto>.Create(items, list.Count, page, pageSize);

        return ServiceResult<PaginatedResult<ProvinceDto>>.Success(result);
    }
}
