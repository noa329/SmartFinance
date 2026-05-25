using System;
using System.Collections.Generic;
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
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public decimal? MonthlyIncome { get; set; }
        public decimal? YearlyIncome { get; set; }
    }
    public class UserCreateDto
    {
        public string? Name { get; set; }
        public string ? Phone { get; set; }
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class UserLoginDto
    {
        public string Email { get; set; } = null!;
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
        public decimal Amount { get; set; }
        public bool IsYearly { get; set; } 
    }
}

