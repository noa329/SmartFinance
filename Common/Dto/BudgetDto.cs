using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        public decimal? CalculatedAmount { get; set; }
        public int Percentage { get; set; }
        public bool AlertEnabled { get; set; }
        public int AlertThreshold { get; set; }
    }

    public class BudgetCreateDto
    {
        [Required(ErrorMessage = "CategoryId הוא שדה חובה")]
        [Range(1, int.MaxValue, ErrorMessage = "CategoryId חייב להיות מספר חיובי")]
        public int CategoryId { get; set; }

        [Range(1, 100, ErrorMessage = "Percentage חייב להיות בין 1 ל-100")]
        public int Percentage { get; set; }
        public bool AlertEnabled { get; set; } = true;

        [Range(1, 100, ErrorMessage = "AlertThreshold חייב להיות בין 1 ל-100")]
        public int AlertThreshold { get; set; } = 80;
    }
    public class BudgetUpdateDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "CategoryId חייב להיות מספר חיובי")]
        public int? CategoryId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Percentage חייב להיות מספר חיובי")]
        public int? Percentage { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "CalculatedAmount חייב להיות גדול מ-0")]
        public decimal? CalculatedAmount { get; set; }
        public bool? AlertEnabled { get; set; }

        [Range(1, 100, ErrorMessage = "AlertThreshold חייב להיות בין 1 ל-100")]
        public int? AlertThreshold { get; set; }
    }
}
