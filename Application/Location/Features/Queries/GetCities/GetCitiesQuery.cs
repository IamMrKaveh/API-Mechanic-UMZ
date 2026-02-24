namespace Application.Location.Features.Queries.GetCities;

public record GetCitiesQuery(int StateId) : IRequest<ServiceResult<IEnumerable<CityDto>>>;

public class CityDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int StateId { get; set; }
}