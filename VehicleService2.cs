public class VehicleService : IVehicleService
{
    private readonly IMemoryCache _memoryCache;
    private readonly HttpClient _httpClient;
    private readonly string cacheKey = "vehicles";
    private readonly string apiUrl = "https://example.com/api/vehicles"; // Replace with your actual API endpoint

    public VehicleService(IMemoryCache memoryCache, HttpClient httpClient)
    {
        _memoryCache = memoryCache;
        _httpClient = httpClient;
    }

    public List<Vehicle> GetVehicles()
    {
        List<Vehicle> vehicles;

        if (!_memoryCache.TryGetValue(cacheKey, out vehicles))
        {
            vehicles = GetVehiclesFromApiAsync().Result;

            _memoryCache.Set(cacheKey, vehicles,
                new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(60))); // Adjust cache duration as needed
        }

        return vehicles;
    }

    private async Task<List<Vehicle>> GetVehiclesFromApiAsync()
    {
        var response = await _httpClient.GetAsync(apiUrl);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var vehicles = JsonSerializer.Deserialize<List<Vehicle>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return vehicles;
        }

        return new List<Vehicle>();
    
