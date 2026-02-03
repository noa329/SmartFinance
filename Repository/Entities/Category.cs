using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Entities
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsFixed { get; set; } 
        public bool IsSystem { get; set; } 

        public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
        public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
    }
}
