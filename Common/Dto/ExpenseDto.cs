using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        public string? EventName { get; set; } 


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
        [Required(ErrorMessage = "Amount הוא שדה חובה")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount חייב להיות גדול מ-0")]
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "CategoryId הוא שדה חובה")]
        [Range(1, int.MaxValue, ErrorMessage = "CategoryId חייב להיות מספר חיובי")]
        public int CategoryId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "EventId חייב להיות מספר חיובי")]
        public int? EventId { get; set; }

        [StringLength(100, ErrorMessage = "EventName לא יכול לעלות על 100 תווים")]
        public string? EventName { get; set; }

        [StringLength(100, ErrorMessage = "MerchantName לא יכול לעלות על 100 תווים")]
        public string MerchantName { get; set; } = string.Empty;

        public bool IsRecurring { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [Range(1, 31, ErrorMessage = "DayOfMonth חייב להיות בין 1 ל-31")]
        public int? DayOfMonth { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "InstallmentAmount חייב להיות גדול מ-0")]
        public int InstallmentsCount { get; set; } = 1;
        public decimal InstallmentAmount { get; set; }
        public DateTime? FirstInstallmentDate { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // EndDate חייב להיות אחרי StartDate
            if (StartDate.HasValue && EndDate.HasValue && EndDate <= StartDate)
                yield return new ValidationResult(
                    "EndDate חייב להיות אחרי StartDate",
                    new[] { nameof(EndDate)} 
                );
               
        }  
    }  
    
    public class updateExpenseDto
    {
        public int Id { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Amount חייב להיות גדול מ-0")]
        public decimal Amount { get; set; }

        [StringLength(300, ErrorMessage = "Description לא יכול לעלות על 300 תווים")]
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "CategoryId חייב להיות מספר חיובי")]
        public int CategoryId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "EventId חייב להיות מספר חיובי")]
        public int? EventId { get; set; }

        [StringLength(100, ErrorMessage = "EventName לא יכול לעלות על 100 תווים")]
        public string? EventName { get; set; } 


        public bool IsRecurring { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [Range(1, 31, ErrorMessage = "DayOfMonth חייב להיות בין 1 ל-31")]
        public int? DayOfMonth { get; set; }

        public int InstallmentsCount { get; set; } = 1;
        public decimal InstallmentAmount { get; set; }
        public DateTime? FirstInstallmentDate { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartDate.HasValue && EndDate.HasValue && EndDate <= StartDate)
                yield return new ValidationResult(
                    "EndDate חייב להיות אחרי StartDate",
                    new[] { nameof(EndDate) });
        }
    }

    public class UpdateExpenseEventDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "EventId חייב להיות מספר חיובי")]
        public int? EventId { get; set; } 
    }
}
