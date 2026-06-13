using Application.Location.Contracts;
using Application.Location.Features.Shared;
using Infrastructure.Location.Models;

namespace Infrastructure.Location.Services;

public class LocationService(
    HttpClient httpClient,
    IAuditService auditService) : ILocationService
{
    public async Task<IReadOnlyList<ProvinceDto>> GetProvincesAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<IReadOnlyList<ExternalProvinceApiDto>>("states", cancellationToken: ct);
            if (response is null)
                return [];

            return response
                .Select(p => new ProvinceDto(p.Id, p.Name, p.Code ?? string.Empty))
                .ToList()
                .AsReadOnly();
        }
        catch (Exception)
        {
            await auditService.LogErrorAsync("Failed to fetch provinces from the location API.", ct);
            throw;
        }
    }

    public async Task<IReadOnlyList<CityDto>> GetCitiesByProvinceAsync(string provinceId, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<IReadOnlyList<ExternalCityApiDto>>($"cities?state_id={provinceId}", cancellationToken: ct);
            if (response is null)
                return [];

            return response
                .Select(c => new CityDto(c.Id, c.Name, c.Province ?? string.Empty, c.StateId))
                .ToList()
                .AsReadOnly();
        }
        catch (Exception)
        {
            await auditService.LogErrorAsync("Failed to fetch cities for province {StateId} from the location API.", ct);
            throw;
        }
    }
}