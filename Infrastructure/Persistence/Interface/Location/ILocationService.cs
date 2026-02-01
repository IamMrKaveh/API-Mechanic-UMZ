namespace Infrastructure.Persistence.Interface.Location;

public interface ILocationService
{
    Task<IEnumerable<object>> GetStatesAsync();

    Task<IEnumerable<object>> GetCitiesByStateAsync(int stateId);
}