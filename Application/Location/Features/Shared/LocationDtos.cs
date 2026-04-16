namespace Application.Location.Features.Shared;

public sealed record ProvinceDto(string Name, string Code);

public sealed record CityDto(string Name, string Province);