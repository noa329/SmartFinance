using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Gmail.v1;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repository.Entities;
using Service.interfaces;
using Service.Interfaces;
using Service.Services;
using System.Security.Claims;

namespace SmartFinance.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GmailController : ControllerBase
    {
        private readonly GmailInvoiceService _gmailService;
        private readonly ISyncService _syncService;
        private readonly IUserService _userService;
        private readonly IConfiguration _config;

        public GmailController(
            GmailInvoiceService gmailService,
            ISyncService syncService,
            IUserService userService,
            IConfiguration config)
        {
            _gmailService = gmailService;
            _syncService = syncService;
            _userService = userService;
            _config = config;
        }

        // בדיקת Gmail scan ידנית
        [HttpPost("test-scan")]
        [Authorize]
        public async Task<IActionResult> TestGmailScan()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null) return Unauthorized();
            int userId = int.Parse(userIdClaim);

            var user = await _userService.GetById(userId);
            if (user?.GmailToken == null)
                return BadRequest(new { message = "לא חיברת את חשבון Gmail שלך" });

            var count = await _gmailService.ProcessUserInvoicesAsync(user.GmailToken, userId);
            return Ok(new { processed = count });
        }

        // הפעלת Sync ידנית (לבדיקות)
        [HttpPost("test-sync")]
        [Authorize]
        public async Task<IActionResult> TriggerSync()
        {
            await _syncService.RunDailySyncAsync();
            return Ok(new { message = "Sync completed" });
        }

        [HttpPost("connect-gmail")]
        [Authorize]
        public async Task<IActionResult> ConnectGmail([FromBody] ConnectGmailDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null) return Unauthorized();
            int userId = int.Parse(userIdClaim);

            // 1. הגדרת ה-Flow בצורה נכונה ללא RedirectUri ב-Initializer
            var flow = new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = _config["Authentication:Google:ClientId"]!,
                        ClientSecret = _config["Authentication:Google:ClientSecret"]!
                    },
                    Scopes = new[] { GmailService.Scope.GmailReadonly }
                });

            try
            {
                // 2. ה-RedirectUri מועבר כאן כפרמטר השלישי ("postmessage")
                var token = await flow.ExchangeCodeForTokenAsync(
                    userId.ToString(), // מומלץ להעביר את ה-userId כמזהה הייחודי של הטוקן בתוך ה-flow
                    dto.AuthCode,
                    "postmessage",
                    CancellationToken.None
                );

                if (string.IsNullOrEmpty(token?.RefreshToken))
                {
                    return BadRequest(new { message = "לא התקבל Refresh Token. יש לוודא שאישרת גישה לא מקוונת (offline access) ושהחשבון לא היה מחובר בעבר (אם כן, יש לנתק אותו בהגדרות גוגל ולנסות שוב)." });
                }

                // 3. שמירת ה-refresh token בבסיס הנתונים
                await _userService.SaveGmailRefreshToken(userId, token.RefreshToken);

                return Ok(new { message = "Gmail connected!" });
            }
            catch (Exception ex)
            {
                // הגנה מפני קריסות של השרת במקרה של קוד פג תוקף או שגיאות רשת מול גוגל
                return BadRequest(new { message = $"שגיאה באימות מול גוגל: {ex.Message}" });
            }
        }

        public class ConnectGmailDto
        {
            public string AuthCode { get; set; } = null!;
        }
    }
}

//using Google.Apis.Auth.OAuth2;
//using Google.Apis.Auth.OAuth2.Flows;
//using Google.Apis.Gmail.v1;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Repository.Entities;
//using Service.interfaces;
//using Service.Interfaces;
//using Service.Services;
//using System.Security.Claims;

//namespace SmartFinance.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class GmailController : ControllerBase
//    {
//        private readonly GmailInvoiceService _gmailService;
//        private readonly ISyncService _syncService;
//        private readonly IUserService _userService;
//        private readonly IConfiguration _config;

//        public GmailController(
//            GmailInvoiceService gmailService,
//            ISyncService syncService,
//            IUserService userService,
//            IConfiguration config)
//        {
//            _gmailService = gmailService;
//            _syncService = syncService;
//            _userService = userService;
//            _config = config;
//        }

//        // בדיקת Gmail scan ידנית
//        [HttpPost("test-scan")]
//        [Authorize]
//        public async Task<IActionResult> TestGmailScan()
//        {
//            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
//            if (userIdClaim == null) return Unauthorized();
//            int userId = int.Parse(userIdClaim);

//            var user = await _userService.GetById(userId);
//            if (user?.GmailToken == null)
//                return BadRequest(new { message = "לא חיברת את חשבון Gmail שלך" });

//            var count = await _gmailService.ProcessUserInvoicesAsync(user.GmailToken, userId);
//            return Ok(new { processed = count });
//        }

//        // הפעלת Sync ידנית (לבדיקות)
//        [HttpPost("test-sync")]
//        [Authorize]
//        public async Task<IActionResult> TriggerSync()
//        {
//            await _syncService.RunDailySyncAsync();
//            return Ok(new { message = "Sync completed" });
//        }

//        [HttpPost("connect-gmail")]
//        [Authorize]
//        public async Task<IActionResult> ConnectGmail([FromBody] ConnectGmailDto dto)
//        {
//            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
//            if (userIdClaim == null) return Unauthorized();
//            int userId = int.Parse(userIdClaim);

//            var flow = new GoogleAuthorizationCodeFlow(
//                new GoogleAuthorizationCodeFlow.Initializer
//                {
//                    ClientSecrets = new ClientSecrets
//                    {
//                        ClientId = _config["Authentication:Google:ClientId"]!,
//                        ClientSecret = _config["Authentication:Google:ClientSecret"]!
//                    },
//                    Scopes = new[] { GmailService.Scope.GmailReadonly }
//                });

//            // מחליף את ה-code ב-refresh token
//            var token = await flow.ExchangeCodeForTokenAsync(
//                "", dto.AuthCode, "postmessage", CancellationToken.None);

//            if (string.IsNullOrEmpty(token.RefreshToken))
//                return BadRequest("No refresh token – make sure to request offline access");

//            // שומר רק את ה-refresh token
//            await _userService.SaveGmailRefreshToken(userId, token.RefreshToken);

//            return Ok(new { message = "Gmail connected!" });
//        }

//        public class ConnectGmailDto
//        {
//            public string AuthCode { get; set; } = null!;
//        }

//    }
//}
