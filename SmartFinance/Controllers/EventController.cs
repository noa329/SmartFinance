using Common.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using System.Security.Claims;

namespace SmartFinance.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/events")]
    public class EventController : ControllerBase
    {
        private readonly IEventService _service;

        public EventController(IEventService service)
        {
            _service = service;
        }

        // GET: api/events
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            int userId = GetUserId();
            var events = await _service.GetUserEvents(userId);
            return Ok(events);
        }

        // GET: api/events/{id}/stats
        [HttpGet("{id}/stats")]
        public async Task<IActionResult> Stats(int id)
        {
            int userId = GetUserId();
            var stats = await _service.GetEventStats(id, userId);
            return Ok(stats);
        }

        // POST: api/events
        [HttpPost]
        public async Task<IActionResult> Create(EventCreateDto dto)
        {
            int userId = GetUserId();
            var created=await _service.AddEvent(dto, userId);
            return Ok(created);
        }

        // PUT: api/events/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, EventUpdateDto dto)
        {
            int userId = GetUserId();
            var updated = await _service.UpdateEvent(id, dto, userId);
            return Ok(updated);
        }

        // DELETE: api/events/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            int userId = GetUserId();
            await _service.DeleteEvent(id, userId);
            return Ok();
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }
    }
}