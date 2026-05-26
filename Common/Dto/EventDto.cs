using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Dto
{
    public class EventDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;      
        public DateTime StartDate { get; set; }        
        public DateTime EndDate { get; set; }          
        public decimal Budget { get; set; }           

        public List<int> ExpenseIds { get; set; } = new();

        public List<ExpenseDto>? Expenses { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal RemainingBudget { get; set; }
        public double Progress { get; set; }
    }

    public class EventCreateDto: IValidatableObject
    {
        [Required(ErrorMessage = "Name הוא שדה חובה")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name חייב להיות בין 2 ל-100 תווים")]
        public string Name { get; set; } = null!;       
        public DateTime StartDate { get; set; }        
        public DateTime EndDate { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Budget חייב להיות גדול מ-0")]
        public decimal Budget { get; set; }             
        public List<int>? ExpenseIds { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndDate <= StartDate)
                yield return new ValidationResult(
                    "EndDate חייב להיות אחרי StartDate",
                    new[] { nameof(EndDate) });
        }
    }

        public class EventUpdateDto: IValidatableObject
    {
           [StringLength(100, MinimumLength = 2, ErrorMessage = "Name חייב להיות בין 2 ל-100 תווים")]
            public string? Name { get; set; }       
            public DateTime? StartDate { get; set; }        
            public DateTime? EndDate { get; set; }  
            [Range(0.01, double.MaxValue, ErrorMessage = "Budget חייב להיות גדול מ-0")]
            public decimal? Budget { get; set; }             
            public List<int>? ExpenseIds { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartDate.HasValue && EndDate.HasValue && EndDate <= StartDate)
                yield return new ValidationResult(
                    "EndDate חייב להיות אחרי StartDate",
                    new[] { nameof(EndDate) });
        }
    }

}
