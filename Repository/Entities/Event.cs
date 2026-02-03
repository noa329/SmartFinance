using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Entities
{
    public class Event
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }

        public string Name { get; set; }
        public decimal Budget { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    }
}
