namespace Application.Location.Features.Queries.GetCities;

public record GetStatesQuery : IRequest<ServiceResult<IEnumerable<StateDto>>>;

public class StateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}