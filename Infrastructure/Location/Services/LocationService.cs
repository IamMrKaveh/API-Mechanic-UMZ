using Application.Location.Contracts;
using Application.Location.Features.Shared;

namespace Infrastructure.Location.Services;

public class LocationService(
    HttpClient httpClient,
    IAuditService auditService) : ILocationService
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<IReadOnlyList<ProvinceDto>> GetProvincesAsync(CancellationToken ct = default)
    {
        _httpClient.BaseAddress = new Uri("https://iran-locations-api.ir/api/v1/fa/");
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IReadOnlyList<ProvinceDto>>("states", cancellationToken: ct);
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
        _httpClient.BaseAddress = new Uri("https://iran-locations-api.ir/api/v1/fa/");
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IReadOnlyList<CityDto>>($"cities?state_id={provinceId}", cancellationToken: ct);
            return response ?? [];
        }
        catch (Exception)
        {
            await auditService.LogErrorAsync("Failed to fetch cities for province {StateId} from the location API.", ct);
            throw;
        }
    }
}