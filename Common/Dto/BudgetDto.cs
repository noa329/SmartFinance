using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Dto
{
    public class BudgetDto
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public int Percentage { get; set; }
        public bool AlertEnabled { get; set; }
        public int AlertThreshold { get; set; }
    }

    public class BudgetCreateDto
    {
        public int CategoryId { get; set; }
        public int Percentage { get; set; }
        public bool AlertEnabled { get; set; } = true;
        public int AlertThreshold { get; set; } = 80;
    }
    public class BudgetUpdateDto
    {
        public int? CategoryId { get; set; }
        public int? Percentage { get; set; }
        public bool? AlertEnabled { get; set; }
        public int? AlertThreshold { get; set; }
    }
}
