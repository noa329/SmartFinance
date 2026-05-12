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
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var count = await _service.GetUnreadAlertsCount(UserId);
            return Ok(count);
        }

        // POST api/alerts
        [HttpPost]
        public async Task<IActionResult> Create(AlertCreateDto dto)
        {
            await _service.CreateAlert(dto, UserId);
            return Ok(201);
        }

        // PATCH api/alerts/{id}
        [HttpPatch("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                await _service.MarkAsRead(id, UserId);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }
    }
}