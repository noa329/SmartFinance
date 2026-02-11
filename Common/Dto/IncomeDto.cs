using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Dto
{
    public class IncomeDto
    {
        public string Source { get; set; } = null!;
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public bool IsRecurring { get; set; }
    }

    public class IncomeCreateDto
    {
        public int UserId { get; set; }
        public string Source { get; set; } = null!;
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public bool IsRecurring { get; set; }
    }

    public class IncomeUpdateDto
    {
        public string? Source { get; set; }
        public decimal? Amount { get; set; }
        public DateTime? Date { get; set; }
        public bool? IsRecurring { get; set; }
    }
}
