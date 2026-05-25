using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Dto
{
    public class FixedExpenseDto
    {
        public string Name { get; set; } = null!;
        public decimal Amount { get; set; }
        public int? CategoryId { get; set; }
    }
}
