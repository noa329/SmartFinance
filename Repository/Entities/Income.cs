using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Entities
{
    public class Income
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }

        public string Source { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public bool IsRecurring { get; set; }
    }
}
