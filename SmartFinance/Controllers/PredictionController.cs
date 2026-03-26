using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;

namespace SmartFinance.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] 
    public class PredictionController : ControllerBase
    {
        private readonly IPredictionService _predictionService;

        public PredictionController(IPredictionService predictionService)
        {
            _predictionService = predictionService;
        }

        [HttpGet("risk-days")]
        public async Task<IActionResult> GetRiskDays()
        {
            var userId = int.Parse(User.Claims.First(c => c.Type == "id").Value);

            var days = await _predictionService.PredictRiskDays(userId);

            return Ok(new
            {
                RiskDays = days
            });
        }
    }
}