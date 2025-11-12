namespace MainApi.Services.Location
{
    public class LocationService : ILocationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<LocationService> _logger;

        public LocationService(HttpClient httpClient, ILogger<LocationService> logger)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://iran-locations-api.ir/api/v1/fa/");
            _logger = logger;
        }

        public async Task<IEnumerable<object>> GetStatesAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<IEnumerable<object>>("states");
                return response ?? Enumerable.Empty<object>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch states from the location API.");
                throw;
            }
        }

        public async Task<IEnumerable<object>> GetCitiesByStateAsync(int stateId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<IEnumerable<object>>($"cities?state_id={stateId}");
                return response ?? Enumerable.Empty<object>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch cities for state {StateId} from the location API.", stateId);
                throw;
            }
        }
    }
}