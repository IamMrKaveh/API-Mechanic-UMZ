using Application.Location.Contracts;
using Application.Location.Features.Shared;

namespace Application.Location.Features.Queries.GetCities;

public class GetCitiesHandler(ILocationService locationService)
    : IQueryHandler<GetCitiesQuery, IEnumerable<CityDto>>
{
    public async Task<ServiceResult<IEnumerable<CityDto>>> Handle(
        GetCitiesQuery request,
        CancellationToken ct)
    {
        var cities = await locationService.GetCitiesByProvinceAsync(request.StateId.ToString(), ct);
        return ServiceResult<IEnumerable<CityDto>>.Success(cities);
    }
}