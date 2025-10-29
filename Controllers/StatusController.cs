using System;
using System.Threading.Tasks;
using CCEAPI.Model.DTOs;
using CCEAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace CCEAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StatusController : ControllerBase
    {
        private readonly CountryService _countryService;

        public StatusController(CountryService countryService)
        {
            _countryService = countryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetStatus()
        {
            try
            {
                var status = await _countryService.GetStatusAsync();
                return Ok(status);
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