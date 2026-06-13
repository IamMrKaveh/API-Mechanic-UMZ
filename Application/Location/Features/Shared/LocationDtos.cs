namespace Application.Location.Features.Shared;

public sealed record ProvinceDto(int Id, string Name, string Code);

public sealed record CityDto(int Id, string Name, string Province, int StateId);