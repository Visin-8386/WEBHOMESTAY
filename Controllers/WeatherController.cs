using Microsoft.AspNetCore.Mvc;
using WebHS.Services;

namespace WebHS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WeatherController : ControllerBase
    {
        private readonly IWeatherService _weatherService;
        private readonly IHomestayService _homestayService;
        private readonly ILogger<WeatherController> _logger;

        public WeatherController(
            IWeatherService weatherService, 
            IHomestayService homestayService,
            ILogger<WeatherController> logger)
        {
            _weatherService = weatherService;
            _homestayService = homestayService;
            _logger = logger;
        }

        /// <summary>
        /// Get weather information by coordinates (for homestay location)
        /// </summary>
        /// <param name="latitude">Latitude coordinate</param>
        /// <param name="longitude">Longitude coordinate</param>
        /// <returns>Weather information</returns>
        [HttpGet("coordinates")]
        public async Task<IActionResult> GetWeatherByCoordinates(double latitude, double longitude)
        {
            try
            {
                if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Invalid coordinates. Latitude must be between -90 and 90, longitude between -180 and 180." 
                    });
                }

                var weather = await _weatherService.GetWeatherAsync(latitude, longitude);
                
                if (weather != null)
                {
                    return Ok(new { 
                        success = true, 
                        data = weather 
                    });
                }

                return Ok(new { 
                    success = false, 
                    message = "Weather data not available for this location" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching weather data for coordinates: {Lat}, {Lon}", latitude, longitude);
                return StatusCode(500, new { 
                    success = false, 
                    message = "Unable to fetch weather data at this time" 
                });
            }
        }

        /// <summary>
        /// Get weather information by city name
        /// </summary>
        /// <param name="city">City name</param>
        /// <returns>Weather information</returns>
        [HttpGet("city/{city}")]
        public async Task<IActionResult> GetWeatherByCity(string city)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(city))
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "City name is required" 
                    });
                }

                var weather = await _weatherService.GetWeatherAsync(city);
                
                if (weather != null)
                {
                    return Ok(new { 
                        success = true, 
                        data = weather 
                    });
                }

                return Ok(new { 
                    success = false, 
                    message = $"Weather data not available for city: {city}" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching weather data for city: {City}", city);
                return StatusCode(500, new { 
                    success = false, 
                    message = "Unable to fetch weather data at this time" 
                });
            }
        }

        /// <summary>
        /// Get weather for homestay location (combines with homestay data)
        /// </summary>
        /// <param name="homestayId">Homestay ID</param>
        /// <returns>Weather information for homestay location</returns>
        [HttpGet("homestay/{homestayId}")]
        public async Task<IActionResult> GetWeatherForHomestay(int homestayId)
        {
            try
            {
                // Get homestay details first
                var homestay = await _homestayService.GetHomestayByIdAsync(homestayId);
                
                if (homestay == null)
                {
                    return NotFound(new { 
                        success = false, 
                        message = "Homestay not found" 
                    });
                }

                WeatherInfo? weather = null;

                // Try to get weather by coordinates first (more accurate)
                if (homestay.Latitude != 0 && homestay.Longitude != 0)
                {
                    weather = await _weatherService.GetWeatherAsync(
                        (double)homestay.Latitude, 
                        (double)homestay.Longitude);
                }

                // Fallback to city name if coordinates fail
                if (weather == null && !string.IsNullOrEmpty(homestay.City))
                {
                    weather = await _weatherService.GetWeatherAsync(homestay.City);
                }

                if (weather != null)
                {
                    return Ok(new { 
                        success = true, 
                        data = weather,
                        homestay = new {
                            id = homestay.Id,
                            name = homestay.Name,
                            city = homestay.City,
                            address = homestay.Address
                        }
                    });
                }

                return Ok(new { 
                    success = false, 
                    message = $"Weather data not available for {homestay.Name} in {homestay.City}" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching weather data for homestay: {HomestayId}", homestayId);
                return StatusCode(500, new { 
                    success = false, 
                    message = "Unable to fetch weather data at this time" 
                });
            }
        }
    }
}
