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
    [Route("api/expenses")]
    public class ExpenseController : ControllerBase
    {
        private readonly IExpenseService _service;

        public ExpenseController(IExpenseService service)
        {
            _service = service;
        }

        private int UserId =>
                 int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return Ok(await _service.GetUserExpenses(UserId));
        }

        [HttpPost]
        public async Task<IActionResult> Create(createExpenseDto dto)
        {
            await _service.AddExpense(dto, UserId);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteExpense(id, UserId);
            return Ok();
        }
    }
}