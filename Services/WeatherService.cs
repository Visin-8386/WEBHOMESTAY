using System.Text.Json;

namespace WebHS.Services
{
    public interface IWeatherService
    {
        Task<WeatherInfo?> GetWeatherAsync(double latitude, double longitude);
        Task<WeatherInfo?> GetWeatherAsync(string city);
    }

    public class WeatherService : IWeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WeatherService> _logger;
        private readonly string _apiKey;
        private readonly string _baseUrl;

        public WeatherService(HttpClient httpClient, IConfiguration configuration, ILogger<WeatherService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _apiKey = configuration["ExternalAPIs:OpenWeatherMap:ApiKey"] ?? "";
            _baseUrl = configuration["ExternalAPIs:OpenWeatherMap:BaseUrl"] ?? "https://api.openweathermap.org/data/2.5";
        }

        public async Task<WeatherInfo?> GetWeatherAsync(double latitude, double longitude)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogWarning("OpenWeatherMap API key not configured");
                return null;
            }

            try
            {
                var url = $"{_baseUrl}/weather?lat={latitude}&lon={longitude}&appid={_apiKey}&units=metric&lang=vi";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var weatherData = JsonSerializer.Deserialize<OpenWeatherMapResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                    });

                    if (weatherData != null)
                    {
                        return new WeatherInfo
                        {
                            Temperature = Math.Round(weatherData.Main.Temp),
                            Description = weatherData.Weather.FirstOrDefault()?.Description ?? "",
                            Humidity = weatherData.Main.Humidity,
                            WindSpeed = weatherData.Wind.Speed,
                            Icon = weatherData.Weather.FirstOrDefault()?.Icon ?? "",
                            City = weatherData.Name
                        };
                    }
                }
                else
                {
                    _logger.LogWarning("Weather API request failed with status code: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching weather data for coordinates: {Lat}, {Lon}", latitude, longitude);
            }

            return null;
        }

        public async Task<WeatherInfo?> GetWeatherAsync(string city)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogWarning("OpenWeatherMap API key not configured");
                return null;
            }

            try
            {
                var url = $"{_baseUrl}/weather?q={Uri.EscapeDataString(city)}&appid={_apiKey}&units=metric&lang=vi";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var weatherData = JsonSerializer.Deserialize<OpenWeatherMapResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                    });

                    if (weatherData != null)
                    {
                        return new WeatherInfo
                        {
                            Temperature = Math.Round(weatherData.Main.Temp),
                            Description = weatherData.Weather.FirstOrDefault()?.Description ?? "",
                            Humidity = weatherData.Main.Humidity,
                            WindSpeed = weatherData.Wind.Speed,
                            Icon = weatherData.Weather.FirstOrDefault()?.Icon ?? "",
                            City = weatherData.Name
                        };
                    }
                }
                else
                {
                    _logger.LogWarning("Weather API request failed with status code: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching weather data for city: {City}", city);
            }

            return null;
        }
    }

    // Data Transfer Objects
    public class WeatherInfo
    {
        public double Temperature { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Humidity { get; set; }
        public double WindSpeed { get; set; }
        public string Icon { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        
        public string IconUrl => $"https://openweathermap.org/img/wn/{Icon}@2x.png";
        public string TemperatureDisplay => $"{Temperature}°C";
        public string WindSpeedDisplay => $"{WindSpeed} m/s";
        public string HumidityDisplay => $"{Humidity}%";
    }

    // OpenWeatherMap API Response Models
    public class OpenWeatherMapResponse
    {
        public MainData Main { get; set; } = new();
        public List<WeatherData> Weather { get; set; } = new();
        public WindData Wind { get; set; } = new();
        public string Name { get; set; } = string.Empty;
    }

    public class MainData
    {
        public double Temp { get; set; }
        public int Humidity { get; set; }
    }

    public class WeatherData
    {
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }

    public class WindData
    {
        public double Speed { get; set; }
    }
}
