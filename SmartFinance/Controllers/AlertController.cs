using Common.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repository.Entities;
using Service.Interfaces;
using System.Security.Claims;

namespace SmartFinance.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/alerts")]
    public class AlertController : ControllerBase
    {
        private readonly IAlertService _service;

        public AlertController(IAlertService service)
        {
            _service = service;
        }

        private int UserId =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // GET api/alerts
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var alerts = await _service.GetUserAlerts(UserId);
            return Ok(alerts);
        }

        // POST api/alerts
        [HttpPost]
        public async Task<IActionResult> Create(AlertCreateDto dto)
        {
            await _service.CreateAlert(dto, UserId);
            return Ok();
        }

        // PATCH api/alerts/{id}
        [HttpPatch("{id}")]
        public async Task<IActionResult> Read(int id)
        {
            await _service.MarkAsRead(id, UserId);
            return Ok();
        }
    }
}