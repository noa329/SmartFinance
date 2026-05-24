using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Services;
using System.Security.Claims;

namespace SmartFinance.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] 
    public class InvoicesController : ControllerBase
    {
        private readonly GmailInvoiceService _gmailInvoiceService;

        public InvoicesController(GmailInvoiceService gmailInvoiceService)
        {
            _gmailInvoiceService = gmailInvoiceService;
        }

        [HttpPost("sync")]
        public async Task<IActionResult> SyncInvoices([FromBody] string googleToken)
        {
            if (string.IsNullOrEmpty(googleToken))
                return BadRequest("Google Access Token is required.");

            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            try
            {
                await _gmailInvoiceService.ProcessUserInvoicesAsync(googleToken, userId);
                return Ok(new { message = "Invoices synchronized successfully!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}