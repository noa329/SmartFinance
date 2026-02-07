using Repository.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public string PasswordHash { get; set; } = null!;

        public UserRole Role { get; set; } = UserRole.User; 
  

        public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
        public ICollection<Event> Events { get; set; } = new List<Event>();
        public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
        public ICollection<Income> Incomes { get; set; } = new List<Income>();

        public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
    }
}
