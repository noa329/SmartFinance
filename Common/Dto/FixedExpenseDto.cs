using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Dto
{
    public class FixedExpenseDto
    {
        [Required(ErrorMessage = "Name הוא שדה חובה")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name חייב להיות בין 2 ל-100 תווים")]
        public string Name { get; set; } = null!;

        [Range(0.01, double.MaxValue, ErrorMessage = "Amount חייב להיות גדול מ-0")]
        public decimal Amount { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "CategoryId חייב להיות מספר חיובי")]
        public int? CategoryId { get; set; }
    }
}
