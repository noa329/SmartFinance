using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Dto
{
    public class UserDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string Email { get; set; }= null!;
        public DateTime CreatedAt { get; set; } 
        public DateTime LastLoginAt { get; set; }
        public string? GmailToken { get; set; }
        public string? PictureUrl { get; set; }
    }

    public class UserUpdateDto
    {

        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name חייב להיות בין 2 ל-100 תווים")]
        public string? Name { get; set; }

        [Phone(ErrorMessage = "Phone אינו בפורמט תקין")]
        public string? Phone { get; set; }

       [EmailAddress(ErrorMessage = "Email אינו בפורמט תקין")]
        public string? Email { get; set; }
        public decimal? MonthlyIncome { get; set; }
        public decimal? YearlyIncome { get; set; }
    }
    public class UserCreateDto
    {
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name חייב להיות בין 2 ל-100 תווים")]
        public string? Name { get; set; }

        [Phone(ErrorMessage = "Phone אינו בפורמט תקין")]
        public string ? Phone { get; set; }

        [EmailAddress(ErrorMessage = "Email אינו בפורמט תקין")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Password הוא שדה חובה")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password חייב להיות לפחות 6 תווים")]
        public string Password { get; set; } = null!;
    }

    public class UserLoginDto
    {
        [Required(ErrorMessage = "Email הוא שדה חובה")]
        [EmailAddress(ErrorMessage = "Email אינו בפורמט תקין")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Password הוא שדה חובה")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password חייב להיות לפחות 6 תווים")]
        public string Password { get; set; } = null!;
    }
    public class AuthResponseDto
    {
        public UserDto User { get; set; } = null!;
        public string Token { get; set; } = null!;
        public string? GoogleAccessToken { get; set; }
        public string? GmailToken { get; set; }
    }

    public class SetIncomeDto
    {
        [Required(ErrorMessage = "Amount הוא שדה חובה")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount חייב להיות גדול מ-0")]
        public decimal Amount { get; set; }
        public bool IsYearly { get; set; } 
    }
}

