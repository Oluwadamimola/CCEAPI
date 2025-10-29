using System;
using System.Threading.Tasks;
using CCEAPI.Model.DTOs;
using CCEAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace CCEAPI.Controllers
{
    [ApiController]
    [Route("[Controller]")]
    public class CountriesController : ControllerBase
    {
        private readonly CountryService _countryService;
        private readonly IImageService _imageService;

        public CountriesController(CountryService countryService, IImageService imageService)
        {
            _countryService = countryService;
            _imageService = imageService;
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshCountries()
        {
            try
            {
                await _countryService.RefreshCountriesAsync();
                return Ok(new { message = "Countries refreshed successfully" });
            }
            catch (Exception ex)
            {
                // Check if it's an external API error
                if (ex.Message.Contains("Could not fetch data") || 
                    ex.Message.Contains("timed out") ||
                    ex.Message.Contains("API"))
                {
                    return StatusCode(503, new ErrorResponse
                    {
                        Error = "External data source unavailable",
                        Details = ex.Message
                    });
                }

                // Internal server error
                return StatusCode(500, new ErrorResponse
                {
                    Error = "Internal server error",
                    Details = ex.Message
                });
            }
        }
        [HttpGet("refresh")]
        public IActionResult GetRefreshEndpoint()
        {
            return StatusCode(405, new ErrorResponse
            {
                Error = "Method Not Allowed",
                Details = "Please use POST /Countries/refresh instead of GET"
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetCountries(
            [FromQuery] string? region,
            [FromQuery] string? currency,
            [FromQuery] string? sort)
        {
            try
            {
                var countries = await _countryService.GetCountriesAsync(region, currency, sort);
                return Ok(countries);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Error = "Internal server error",
                    Details = ex.Message
                });
            }
        }
        [HttpGet("image")]
        public IActionResult GetSummaryImage()
        {
            try
            {
                var imageBytes = _imageService.GetSummaryImage();
                
                if (imageBytes == null)
                {
                    return NotFound(new ErrorResponse
                    {
                        Error = "Summary image not found"
                    });
                }

                return File(imageBytes, "image/png");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Error = "Internal server error",
                    Details = ex.Message
                });
            }
        }

        [HttpGet("{name}")]
        public async Task<IActionResult> GetCountryByName(string name)
        {
            try
            {
                var country = await _countryService.GetCountryByNameAsync(name);
                
                if (country == null)
                {
                    return NotFound(new ErrorResponse
                    {
                        Error = "Country not found"
                    });
                }

                return Ok(country);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Error = "Internal server error",
                    Details = ex.Message
                });
            }
        }

        [HttpDelete("{name}")]
        public async Task<IActionResult> DeleteCountry(string name)
        {
            try
            {
                var deleted = await _countryService.DeleteCountryAsync(name);
                
                if (!deleted)
                {
                    return NotFound(new ErrorResponse
                    {
                        Error = "Country not found"
                    });
                }

                return Ok(new { message = $"Country '{name}' deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Error = "Internal server error",
                    Details = ex.Message
                });
            }
        }

        
    }
}