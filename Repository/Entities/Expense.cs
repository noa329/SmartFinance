using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Entities
{
    public class Expense
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [Required]
        [MaxLength(150)]
        public string MerchantName { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public DateTime TransactionDate { get; set; }

        [Required]
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public int? EventId { get; set; }
        public Event Event { get; set; } = null!;

        public int InstallmentsCount { get; set; } = 1;
        public decimal InstallmentAmount { get; set; }
        public DateTime FirstInstallmentDate { get; set; }

        [MaxLength(100)]
        public string Source { get; set; } = null!;
        public string OriginalText { get; set; } = null!;
    }
}
