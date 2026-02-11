using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Dto
{
    public class UserDto
    {
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string Email { get; set; }= null!;
        public DateTime CreatedAt { get; set; }
    }

    public class UserAdminDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string Email { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }

    public class UserUpdateDto
    {
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
    }

    public class UserAdminUpdateDto
    {
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
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
}

