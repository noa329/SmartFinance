using Common.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.interfaces;
using Service.Interfaces;
using Service.sevices;

namespace SmartFinance.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BudgetController : ControllerBase
    {
        private readonly IBudgetService service;

        public BudgetController(IBudgetService service)
        {
            this.service = service;
        }

        // GET: api/Budget
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<BudgetDto>>> GetAll()
        {
            try
            {
                var budgets = await service.GetAll();
                return Ok(budgets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        // GET: api/Budget/:id
        [HttpGet("{id}")]
        public async Task<ActionResult<List<BudgetDto>>> GetById(int id)
        {

            try
            {
                var budget = await service.GetById(id);
                return Ok(budget);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }

        }

        // POST: api/Budget
        [HttpPost]
        public async Task<ActionResult<BudgetDto>> Create( [FromBody] BudgetCreateDto budgetDto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
                budgetDto.UserId = userId;
                var createdBudget = await service.Add(budgetDto);
                return CreatedAtAction(nameof(GetById), new { id = createdBudget.Id }, createdBudget);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }

        }

        // PATCH: api/Budget/:id
        [HttpPatch("{id}")]
        public async Task<ActionResult<BudgetDto>> Update(int id, [FromBody] BudgetUpdateDto updateDto)
        {
            try
            {
                var updated = await service.Update(id, updateDto);
                return Ok(updated);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await service.Delete(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }
    }
}
