using Application.Location.Contracts;
using Application.Location.Features.Shared;
using Infrastructure.Location.Models;

namespace Infrastructure.Location.Services;

public sealed class LocationService(HttpClient httpClient, IAuditService auditService) : ILocationService
{
    internal const string ProvincesErrorMessage =
        "Failed to fetch provinces from the location API.";

    internal const string CitiesErrorMessage =
        "Failed to fetch cities for province {StateId} from the location API.";

    private readonly HttpClient _httpClient = httpClient;
    private readonly IAuditService _auditService = auditService;

    public async Task<IReadOnlyList<ProvinceDto>> GetProvincesAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient
                .GetFromJsonAsync<IReadOnlyList<ExternalProvinceApiDto>>("states", cancellationToken: ct);

            if (response is null) return Array.Empty<ProvinceDto>();

            return response
                .Select(p => new ProvinceDto(p.Id, p.Name, p.Code ?? string.Empty))
                .ToList()
                .AsReadOnly();
        }
        catch (Exception ex) when (ex is OperationCanceledException { InnerException: HttpRequestException })
        {
            await _auditService.LogErrorAsync(ProvincesErrorMessage, ct);
            throw ex.InnerException!;
        }
        catch
        {
            await _auditService.LogErrorAsync(ProvincesErrorMessage, ct);
            throw;
        }
    }

    public async Task<IReadOnlyList<CityDto>> GetCitiesByProvinceAsync(string provinceId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient
                .GetFromJsonAsync<IReadOnlyList<ExternalCityApiDto>>(
                    $"cities?state_id={Uri.EscapeDataString(provinceId)}", cancellationToken: ct);

            if (response is null) return [];

            return response
                .Select(c => new CityDto(c.Id, c.Name, c.Province ?? string.Empty, c.StateId))
                .ToList()
                .AsReadOnly();
        }
        catch (Exception ex) when (ex is OperationCanceledException { InnerException: HttpRequestException })
        {
            await _auditService.LogErrorAsync(CitiesErrorMessage, ct);
            throw ex.InnerException!;
        }
        catch
        {
            await _auditService.LogErrorAsync(CitiesErrorMessage, ct);
            throw;
        }
    }
}
