using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using System.Security.Claims;

namespace SmartFinance.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/receipts")]
    public class ReceiptController : ControllerBase
    {
        private readonly IReceiptProcessorService _receiptProcessor;

        public ReceiptController(IReceiptProcessorService receiptProcessor)
        {
            _receiptProcessor = receiptProcessor;
        }

        // POST: api/receipts/process
        [HttpPost("process")]
        public async Task<IActionResult> ProcessReceipt([FromBody] ProcessReceiptRequest request)
        {
            try
            {
                int userId = GetUserId();

                await _receiptProcessor.ProcessReceipt(request.ReceiptText, userId);

                return Ok(new { success = true, message = "Receipt processed successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }
    }

    public class ProcessReceiptRequest
    {
        public string ReceiptText { get; set; } = string.Empty;
    }
}