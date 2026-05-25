// ============================================================
//  SmartFinance/Controllers/InitialLoginController.cs
// ============================================================
using Common.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using System.Security.Claims;

namespace SmartFinance.Controllers
{
    [ApiController]
    [Route("api/initial")]
    [Authorize]
    public class InitialLoginController : ControllerBase
    {
        private readonly IInitialLoginService _service;

        public InitialLoginController(IInitialLoginService service)
        {
            _service = service;
        }

        private int UserId =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpPost("income")]
        public async Task<IActionResult> SetIncome([FromBody] SetIncomeDto dto)
        {
            await _service.SetInitialIncome(UserId, dto.Amount, dto.IsYearly);
            return Ok(new { success = true });
        }

        [HttpPost("categories/init")]
        public async Task<IActionResult> InitCategories()
        {
            await _service.InitializeUserCategories(UserId);
            return Ok(new { message = "Categories initialized successfully" });
        }

        [HttpGet("budget/calculate")]
        public async Task<ActionResult<List<BudgetDto>>> CalculateBudget()
        {
            var result = await _service.CalculateBudget(UserId);
            return Ok(result);
        }

        [HttpGet("budget/calculate-monthly")]
        public async Task<ActionResult<List<BudgetDto>>> CalculateBudgetMonthly([FromQuery] int? year, [FromQuery] int? month)
        {
            DateTime? targetMonth = null;
            if (year.HasValue && month.HasValue)
            {
                targetMonth = new DateTime(year.Value, month.Value, 1);
            }

            var result = await _service.CalculateBudgetMonthly(UserId, targetMonth);
            return Ok(result);
        }

        // ─── END POINT חדש שנוסף כעת ───
        [HttpPost("fixed-expenses")]
        public async Task<IActionResult> SetFixedExpenses([FromBody] List<FixedExpenseDto> expenses)
        {
            if (expenses == null || expenses.Count == 0)
                return Ok(new { saved = 0 });

            await _service.SaveFixedExpenses(UserId, expenses);
            return Ok(new { saved = expenses.Count });
        }
    }
}