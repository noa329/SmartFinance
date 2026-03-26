using Common.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.interfaces;
using Service.Interfaces;
using System.Security.Claims;

namespace SmartFinance.Controllers
{
    namespace SmartFinance.Controllers
    {
        [Authorize]
        [Route("api/[controller]")]
        [ApiController]
        public class BudgetController : ControllerBase
        {
            private readonly IBudgetService service;

            public BudgetController(IBudgetService service)
            {
                this.service = service;
            }
            private int UserId =>
                  int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);


            [HttpGet]
            public async Task<ActionResult<List<BudgetDto>>> GetUserBudgets()
            {
                var budgets = await service.GetUserBudgets(UserId);
                return Ok(budgets);
            }

            [HttpGet("{id}")]
            public async Task<ActionResult<BudgetDto>> GetById(int id)
            {
                var budget = await service.GetById(id, UserId);
                return Ok(budget);
            }

            [HttpPost]
            public async Task<ActionResult<BudgetDto>> Create(BudgetCreateDto dto)
            {
                var created = await service.Add(dto, UserId);

                return CreatedAtAction(nameof(GetById),
                    new { id = created.Id },
                    created);
            }

            [HttpPatch("{id}")]
            public async Task<ActionResult<BudgetDto>> Update(int id, BudgetUpdateDto dto)
            {
                var updated = await service.Update(id, dto, UserId);
                return Ok(updated);
            }

            [HttpDelete("{id}")]
            public async Task<IActionResult> Delete(int id)
            {
                await service.Delete(id, UserId);
                return NoContent();
            }

        }
    }
}
