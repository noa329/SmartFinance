using Common.Dto;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.interfaces;
using Service.Services;
using System.Security.Claims;


namespace SmartFinance.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userService.GetAll();
            return Ok(users);
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var userId = GetUserId();
            var user = await _userService.GetById(userId);

            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [Authorize]
        [HttpPut("me")]
        public async Task<IActionResult> Update(UserUpdateDto dto)
        {
            var userId = GetUserId();
            var updated = await _userService.Update(userId, dto);
            return Ok(updated);
        }

        [Authorize]
        [HttpDelete("me")]
        public async Task<IActionResult> Delete()
        {
            var userId = GetUserId();
            await _userService.Delete(userId);
            return NoContent();
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserCreateDto dto)
        {
            var result = await _userService.Register(dto);
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginDto dto)
        {
            var result = await _userService.Login(dto);

            if (result == null)
                return Unauthorized();

            return Ok(result);
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.IdToken))
                return BadRequest("Google token is required");

            try
            {
                // רק אימות זהות – בלי Gmail בכלל!
                var result = await _userService.GoogleLoginOrRegister(request.IdToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Invalid Google Token", error = ex.Message });
            }
        }

        //[HttpPost("google-login")]
        //public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        //{
        //    if (request == null || string.IsNullOrEmpty(request.IdToken))
        //        return BadRequest("Google token is required");

        //    try
        //    {
        //        // מעבירים את ה-Token לשירות שלך שמפענח אותו מול גוגל
        //        var result = await _userService.GoogleLoginOrRegister(request.IdToken);
        //        return Ok(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new { message = "Invalid Google Token", error = ex.Message });
        //    }
        //}

       // מחלקה קטנה לקבלת הבקשה
        public class GoogleLoginRequest
        {
            public string? IdToken { get; set; }
        }


        //    // בתוך ה-Controller, אחרי שקיבלת את ה-googleToken (שהוא ה-Access Token מה-React החדש):
        //    [HttpPost("google-login")]
        //public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        //{
        //    try
        //    {
        //        // 1. מייצרים אובייקט של הרשאות מגוגל באמצעות הטוקן שקיבלנו מה-React
        //        var credential = GoogleCredential.FromAccessToken(request.IdToken);

        //        // 2. פותחים את שירות ה-Gmail הרשמי (מהחבילה שהתקנת)
        //        var gmailService = new GmailService(new BaseClientService.Initializer()
        //        {
        //            HttpClientInitializer = credential,
        //            ApplicationName = "Smart Finance",
        //        });

        //        // 3. מבקשים מגוגל את רשימת 5 המיילים האחרונים של המשתמש המחובר ("me")
        //        var emailListRequest = gmailService.Users.Messages.List("me");
        //        emailListRequest.MaxResults = 5;
        //        var emailListResponse = await emailListRequest.ExecuteAsync();

        //        // בדיקה: האם חזרו מיילים?
        //        if (emailListResponse.Messages != null && emailListResponse.Messages.Count > 0)
        //        {
        //            System.Diagnostics.Debug.WriteLine("הצלחנו להתחבר ולקרוא את רשימת המיילים!");

        //            // לולאה קטנה שמדפיסה את ה-ID של המיילים לחלון ה-Output ב-Visual Studio
        //            foreach (var message in emailListResponse.Messages)
        //            {
        //                System.Diagnostics.Debug.WriteLine($"Message ID: {message.Id}");
        //            }
        //        }
        //        else
        //        {
        //            System.Diagnostics.Debug.WriteLine("החיבור הצליח, אך תיבת המייל ריקה.");
        //        }

        //        // כאן ממשיך הקוד הרגיל שלך שמחזיר את הלוגין למשתמש...
        //        var result = await _userService.GoogleLoginOrRegister(request.IdToken);
        //        return Ok(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new { message = "שגיאה בקריאת המיילים", error = ex.Message });
        //    }
        //}
    }
}