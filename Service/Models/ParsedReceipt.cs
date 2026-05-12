using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models
{
    public class ParsedReceipt
    {
        // Basic Info
        public string MerchantName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime TransactionDate { get; set; }
        
        // Payment Details
        public bool HasInstallments { get; set; }
        public int InstallmentsCount { get; set; } = 1;
        public decimal InstallmentAmount { get; set; }
        public DateTime? FirstInstallmentDate { get; set; }
        
        // Recurring
        public bool IsRecurring { get; set; }
        public string RecurringFrequency { get; set; } = string.Empty; // "חודשי", "שנתי"
        
        // Metadata
        public string OriginalText { get; set; } = string.Empty;
        public string Currency { get; set; } = "ILS";
        public string Last4Digits { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty; // "אשראי", "חיוב מיידי"
        
        // Confidence
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
