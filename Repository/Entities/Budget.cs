using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Entities
{
    public class Budget
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; }

        public int Percentage { get; set; } // אחוז מסכום הפנוי
        public bool AlertEnabled { get; set; } = true;
        public int AlertThreshold { get; set; } = 80; // ברירת מחדל 80%
    }
}
