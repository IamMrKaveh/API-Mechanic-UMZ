namespace Application.Location.Contracts;

public interface ILocationService
{
    Task<IReadOnlyList<ProvinceDto>> GetProvincesAsync(CancellationToken ct = default);

    Task<IReadOnlyList<CityDto>> GetCitiesByProvinceAsync(string province, CancellationToken ct = default);
}

public record ProvinceDto(string Name, string Code);
public record CityDto(string Name, string Province);