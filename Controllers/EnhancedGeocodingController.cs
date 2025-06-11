using Microsoft.AspNetCore.Mvc;
using WebHS.Services.Enhanced;

namespace WebHS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EnhancedGeocodingController : ControllerBase
    {
        private readonly EnhancedGeocodingService _geocodingService;

        public EnhancedGeocodingController(EnhancedGeocodingService geocodingService)
        {
            _geocodingService = geocodingService;
        }

        [HttpGet("coordinates")]
        [HttpPost("coordinates")]
        public async Task<IActionResult> GetCoordinates([FromQuery] string? address, [FromBody] EnhancedGeocodingRequest? request = null)
        {
            // Handle both query parameter and request body
            var addressToUse = address ?? request?.Address;
            
            if (string.IsNullOrWhiteSpace(addressToUse))
            {
                return BadRequest(new { success = false, message = "Address is required" });
            }

            try
            {
                var (latitude, longitude) = await _geocodingService.GetCoordinatesAsync(addressToUse);

                if (latitude.HasValue && longitude.HasValue)
                {
                    return Ok(new { 
                        success = true, 
                        latitude = latitude.Value, 
                        longitude = longitude.Value, 
                        displayName = addressToUse 
                    });
                }

                return Ok(new { success = false, message = "Coordinates not found for the given address" });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("address")]
        [HttpPost("address")]
        public async Task<IActionResult> GetEnhancedAddress([FromQuery] double? latitude, [FromQuery] double? longitude, [FromBody] EnhancedReverseGeocodingRequest? request = null)
        {
            // Handle both query parameters and request body
            var lat = latitude ?? request?.Latitude;
            var lng = longitude ?? request?.Longitude;
            
            if (!lat.HasValue || !lng.HasValue)
            {
                return BadRequest(new { success = false, message = "Latitude and longitude are required" });
            }

            try
            {
                var addressResponse = await _geocodingService.GetEnhancedAddressFromCoordinatesAsync(lat.Value, lng.Value);

                if (addressResponse != null && addressResponse.Success)
                {
                    return Ok(new { 
                        success = true, 
                        address = addressResponse.FormattedAddress,
                        displayName = addressResponse.DisplayName,
                        components = new {
                            houseNumber = addressResponse.HouseNumber,
                            streetName = addressResponse.StreetName,
                            ward = addressResponse.Ward,
                            district = addressResponse.District,
                            province = addressResponse.Province,
                            country = addressResponse.Country,
                            postCode = addressResponse.PostCode
                        },
                        rawData = addressResponse.RawData, // 🎯 ADD RAW DATA FROM NOMINATIM
                        source = addressResponse.Source,
                        latitude = lat.Value, 
                        longitude = lng.Value 
                    });
                }

                return Ok(new { success = false, message = "Address not found for the given coordinates" });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("test")]
        public async Task<IActionResult> TestNominatimFormat([FromQuery] double latitude = 10.7769, [FromQuery] double longitude = 106.7009)
        {
            try
            {
                var result = await _geocodingService.GetEnhancedAddressFromCoordinatesAsync(latitude, longitude);
                
                return Ok(new { 
                    success = true,
                    testCoordinates = new { latitude, longitude },
                    result = result,
                    message = "Enhanced geocoding test completed" 
                });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, error = ex.Message });
            }
        }
    }

    public class EnhancedGeocodingRequest
    {
        public string? Address { get; set; }
    }

    public class EnhancedReverseGeocodingRequest
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
