namespace Application.Location.Contracts;

public interface ILocationService
{
    Task<IEnumerable<StateDto>> GetStatesAsync(CancellationToken ct = default);

    Task<IEnumerable<CityDto>> GetCitiesByStateAsync(int stateId, CancellationToken ct = default);
}

public class StateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CityDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int StateId { get; set; }
}