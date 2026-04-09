using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Dto
{
    public class ExpenseDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int CategoryId { get; set; }
        public int? EventId { get; set; }

        public bool IsRecurring { get; set; }              
        public DateTime? StartDate { get; set; }           
        public DateTime? EndDate { get; set; }             
        public int? DayOfMonth { get; set; }               

        public int InstallmentsCount { get; set; } = 1;   
        public decimal InstallmentAmount { get; set; }    
        public DateTime? FirstInstallmentDate { get; set; } 
    }

    public class createExpenseDto
    {
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int CategoryId { get; set; }
        public int? EventId { get; set; }
        public string MerchantName { get; set; } = string.Empty;

        public bool IsRecurring { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? DayOfMonth { get; set; }

        public int InstallmentsCount { get; set; } = 1;
        public decimal InstallmentAmount { get; set; }
        public DateTime? FirstInstallmentDate { get; set; }
    }

    public class updateExpenseDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int CategoryId { get; set; }
        public int? EventId { get; set; }

        public bool IsRecurring { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? DayOfMonth { get; set; }

        public int InstallmentsCount { get; set; } = 1;
        public decimal InstallmentAmount { get; set; }
        public DateTime? FirstInstallmentDate { get; set; }
    }
}
