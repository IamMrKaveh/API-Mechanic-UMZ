using Application.Location.Features.Shared;

namespace Application.Location.Contracts;

public interface ILocationService
{
    Task<IReadOnlyList<ProvinceDto>> GetProvincesAsync(CancellationToken ct = default);

    Task<IReadOnlyList<CityDto>> GetCitiesByProvinceAsync(
        string province,
        CancellationToken ct = default);
}