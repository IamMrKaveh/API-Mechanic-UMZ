namespace Application.Location.Features.Shared;

public sealed record StateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed record ProvinceDto(string Name, string Code);

public sealed record CityDto(string Name, string Province);