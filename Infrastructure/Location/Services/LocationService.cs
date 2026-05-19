using Application.Location.Contracts;
using Application.Location.Features.Shared;

namespace Infrastructure.Location.Services;

public class LocationService(
    HttpClient httpClient,
    IAuditService auditService) : ILocationService
{
    public async Task<IReadOnlyList<ProvinceDto>> GetProvincesAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<IReadOnlyList<ProvinceDto>>("states", cancellationToken: ct);
            return response ?? [];
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
            var response = await httpClient.GetFromJsonAsync<IReadOnlyList<CityDto>>($"cities?state_id={provinceId}", cancellationToken: ct);
            return response ?? [];
        }
        catch (Exception)
        {
            await auditService.LogErrorAsync("Failed to fetch cities for province {StateId} from the location API.", ct);
            throw;
        }
    }
}