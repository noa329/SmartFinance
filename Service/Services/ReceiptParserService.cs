// Services/ReceiptParserService.cs
using Service.Interfaces;
using Service.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Service.Services
{
    public class ReceiptParserService : IReceiptParserService
    {
        public async Task<ParsedReceipt> ParseReceiptText(string text)
        {
            Console.WriteLine("\n[ReceiptParser] ========================================");
            Console.WriteLine("[ReceiptParser] Starting to parse receipt...");
            Console.WriteLine($"[ReceiptParser] Original text:\n{text}");
            Console.WriteLine("[ReceiptParser] ========================================");

            var receipt = new ParsedReceipt
            {
                OriginalText = text,
                IsValid = false
            };

            try
            {
                // ✅ שלב 1: ניקוי וNormalization
                var cleanText = NormalizeText(text);
                Console.WriteLine($"\n[ReceiptParser] Normalized text:\n{cleanText}\n");

                // ✅ שלב 2: חילוץ נתונים
                receipt.MerchantName = ExtractMerchantName(cleanText);
                receipt.TotalAmount = ExtractTotalAmount(cleanText);
                receipt.TransactionDate = ExtractDate(cleanText);
                receipt.Last4Digits = ExtractLast4Digits(cleanText);
                receipt.PaymentMethod = ExtractPaymentMethod(cleanText);

                // ✅ שלב 3: תשלומים
                var installments = ExtractInstallments(cleanText);
                receipt.HasInstallments = installments.hasInstallments;
                receipt.InstallmentsCount = installments.count;
                receipt.InstallmentAmount = installments.amount;

                // אם יש תשלומים - תאריך תשלום ראשון זה התאריך של העסקה
                receipt.FirstInstallmentDate = installments.hasInstallments
                    ? receipt.TransactionDate
                    : null;

                // ✅ שלב 4: הוצאה קבועה
                receipt.IsRecurring = DetectRecurring(cleanText);
                receipt.RecurringFrequency = ExtractRecurringFrequency(cleanText);

                // ✅ שלב 5: Validation
                receipt.IsValid = ValidateReceipt(receipt);

                // ✅ הדפסת תוצאות
                PrintResults(receipt);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReceiptParser] ❌ Exception: {ex.Message}");
                Console.WriteLine($"[ReceiptParser] Stack: {ex.StackTrace}");
                receipt.Errors.Add($"Parsing error: {ex.Message}");
            }

            return receipt;
        }

        // ========================================
        // ✅ שיפור: Normalization מתקדם
        // ========================================
        private string NormalizeText(string text)
        {
            // המרת כל הווריאציות של שקלים ל-₪
            text = text.Replace("ש\"ח", "₪");
            text = text.Replace("שח", "₪");
            text = text.Replace("ש״ח", "₪");
            text = text.Replace("NIS", "₪");

            // רווחים מיותרים
            text = Regex.Replace(text, @"\s+", " ");

            // ניקוי
            text = text.Trim();

            return text;
        }

        // ========================================
        // ✅ שיפור: חילוץ שם עסק - גרסה מתקדמת
        // ========================================

        private string ExtractMerchantName(string text)
        {
            Console.WriteLine("[ReceiptParser] Extracting merchant name...");

            // ✅ תיקון: Regex ספציפי יותר - תופס רק מה שמיד אחרי "ב-"
            var patterns = new[]
            {
        // דפוס 1: "עסקה ב-רמי לוי" - תופס עד רווח או סימן פיסוק
        @"(?:עסקה|חיוב|רכישה|ביצעת עסקה)\s+ב[-\s:]?([א-תA-Za-z0-9""'\-\.& ]+?)(?:\s+(?:תאריך|סכום|בתאריך|ב|ע""ס|על|של)|$)",
        
        // דפוס 2: "ב-רמי לוי תאריך" - תופס עד מילת מפתח
        @"ב[-\s:]([א-תA-Za-z0-9""'\-\.& ]+?)(?:\s+(?:תאריך|סכום|בתאריך|ע""ס|על|של|כרטיס)|$)",
        
        // דפוס 3: אנגלית
        @"(?:at|in)\s+([A-Za-z0-9 \-\.&]+?)(?:\s+(?:date|amount|on)|$)",
        
        // דפוס 4: fallback - שורה ראשונה עד נקודותיים או מקף
        @"^([א-תA-Za-z0-9 \-\.&]{3,40})(?:\s*[:\-]|\s+(?:עסקה|תאריך|סכום))",
    };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.RightToLeft | RegexOptions.IgnoreCase);
                if (match.Success && match.Groups.Count > 1)
                {
                    var name = match.Groups[1].Value.Trim();

                    // ✅ ניקוי סופי
                    name = name.TrimEnd('-', '.', ':', ' ', ',');

                    // ✅ בדיקות נוספות - לא מספרים, לא תאריכים, לא "כרטיס"
                    if (!IsNumeric(name) &&
                        !name.Contains("תאריך") &&
                        !name.Contains("סכום") &&
                        !name.Contains("כרטיס") &&
                        !Regex.IsMatch(name, @"^\d{4}$") && // לא 4 ספרות
                        name.Length >= 2)
                    {
                        Console.WriteLine($"[ReceiptParser]   ✅ Found: '{name}' (pattern: {pattern.Substring(0, 30)}...)");
                        return name;
                    }
                    else
                    {
                        Console.WriteLine($"[ReceiptParser]   ⏭️ Rejected: '{name}' (invalid merchant name)");
                    }
                }
            }

            Console.WriteLine("[ReceiptParser]   ⚠️ Merchant not found, using 'לא זוהה'");
            return "לא זוהה";
        }

        // ========================================
        // ✅ שיפור: חילוץ סכום - גרסה חזקה
        // ========================================
        private decimal ExtractTotalAmount(string text)
        {
            Console.WriteLine("[ReceiptParser] Extracting total amount...");

            var patterns = new[]
            {
                // "סכום: 287.50 ₪"
                @"(?:סכום|סה""כ|ע""ס|על סך|חיוב של|של)\s*[:\-]?\s*(?:₪)?\s*([\d,]+(?:\.\d{1,2})?)\s*(?:₪)?",
                
                // "₪287.50"
                @"₪\s*([\d,]+(?:\.\d{1,2})?)",
                
                // "287.50 ₪"
                @"([\d,]+(?:\.\d{1,2})?)\s*₪",
                
                // fallback - כל מספר עשרוני
                @"\b([\d,]+\.\d{2})\b",
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern);
                if (match.Success && match.Groups.Count > 1)
                {
                    var amountStr = match.Groups[1].Value.Replace(",", "");
                    if (decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
                    {
                        // בדיקת סבירות (בין 0.01 ל-100,000)
                        if (amount >= 0.01m && amount <= 100000m)
                        {
                            Console.WriteLine($"[ReceiptParser]   ✅ Found: {amount:N2} ₪");
                            return amount;
                        }
                    }
                }
            }

            Console.WriteLine("[ReceiptParser]   ⚠️ Amount not found, returning 0");
            return 0;
        }

        // ========================================
        // ✅ שיפור: חילוץ תאריך - תומך בכל הפורמטים
        // ========================================
        private DateTime ExtractDate(string text)
        {
            Console.WriteLine("[ReceiptParser] Extracting date...");

            var patterns = new[]
            {
                // "תאריך: 15/04/2026"
                @"(?:תאריך|בתאריך)[:\s]*(\d{1,2}[\/\.\-]\d{1,2}[\/\.\-]\d{2,4})",
                
                // "15/04/2026" או "15.04.26"
                @"(\d{1,2}[\/\.\-]\d{1,2}[\/\.\-]\d{2,4})",
                
                // "2026-04-15"
                @"(\d{4}[\/\.\-]\d{1,2}[\/\.\-]\d{1,2})",
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern);
                if (match.Success && match.Groups.Count > 1)
                {
                    var dateStr = match.Groups[1].Value;

                    var formats = new[]
                    {
                        "dd/MM/yyyy", "dd.MM.yyyy", "dd-MM-yyyy",
                        "yyyy/MM/dd", "yyyy.MM.dd", "yyyy-MM-dd",
                        "dd/MM/yy", "dd.MM.yy", "dd-MM-yy",
                        "d/M/yyyy", "d.M.yyyy", "d-M-yyyy",
                    };

                    foreach (var format in formats)
                    {
                        if (DateTime.TryParseExact(dateStr, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                        {
                            // בדיקת סבירות (לא עתיד רחוק, לא עבר רחוק)
                            if (date >= DateTime.Today.AddYears(-5) && date <= DateTime.Today.AddDays(7))
                            {
                                Console.WriteLine($"[ReceiptParser]   ✅ Found: {date:dd/MM/yyyy}");
                                return date;
                            }
                        }
                    }
                }
            }

            Console.WriteLine("[ReceiptParser]   ⚠️ Date not found, using today");
            return DateTime.Today;
        }

        // ========================================
        // חילוץ 4 ספרות
        // ========================================
        private string ExtractLast4Digits(string text)
        {
            var patterns = new[]
            {
                @"(?:כרטיס|card).*?(?:מסתיים ב|ending with)?\s*(\d{4})",
                @"\*{2,}\s*(\d{4})",
                @"xxxx\s*(\d{4})",
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (match.Success && match.Groups.Count > 1)
                {
                    return match.Groups[1].Value;
                }
            }

            return string.Empty;
        }

        // ========================================
        // חילוץ אמצעי תשלום
        // ========================================
        private string ExtractPaymentMethod(string text)
        {
            if (text.Contains("אשראי", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("credit", StringComparison.OrdinalIgnoreCase))
                return "אשראי";

            if (text.Contains("חיוב מיידי", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("דביט", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("debit", StringComparison.OrdinalIgnoreCase))
                return "חיוב מיידי";

            return "אשראי"; // ברירת מחדל
        }

        // ========================================
        // ✅ שיפור: חילוץ תשלומים
        // ========================================
        private (bool hasInstallments, int count, decimal amount) ExtractInstallments(string text)
        {
            Console.WriteLine("[ReceiptParser] Checking for installments...");

            var patterns = new[]
            {
                // "12 תשלומים של 150 ₪"
                @"(\d+)\s*תשלומים?\s*(?:של)?\s*(?:₪)?\s*([\d,]+(?:\.\d{1,2})?)",
                
                // "תשלום 1 מתוך 12"
                @"תשלום\s*1\s*מתוך\s*(\d+)",
                
                // "12x150"
                @"(\d+)\s*x\s*(?:₪)?\s*([\d,]+(?:\.\d{1,2})?)",
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern);
                if (match.Success)
                {
                    // תשלום X מתוך Y
                    if (match.Groups.Count == 2)
                    {
                        if (int.TryParse(match.Groups[1].Value, out var count) && count > 1)
                        {
                            Console.WriteLine($"[ReceiptParser]   ✅ Found: {count} installments (amount unknown)");
                            return (true, count, 0);
                        }
                    }

                    // X תשלומים של Y
                    if (match.Groups.Count > 2)
                    {
                        var countStr = match.Groups[1].Value;
                        var amountStr = match.Groups[2].Value.Replace(",", "");

                        if (int.TryParse(countStr, out var count) && count > 1 &&
                            decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
                        {
                            Console.WriteLine($"[ReceiptParser]   ✅ Found: {count}x{amount:N2}");
                            return (true, count, amount);
                        }
                    }
                }
            }

            Console.WriteLine("[ReceiptParser]   No installments detected");
            return (false, 1, 0);
        }

        // ========================================
        // זיהוי הוצאה קבועה
        // ========================================
        private bool DetectRecurring(string text)
        {
            var recurringKeywords = new[]
            {
                "מנוי", "חיוב חודשי", "חודשי", "שנתי",
                "הוראת קבע", "recurring", "subscription",
                "monthly", "yearly", "auto debit"
            };

            return recurringKeywords.Any(keyword =>
                text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        private string ExtractRecurringFrequency(string text)
        {
            if (text.Contains("חודשי", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("monthly", StringComparison.OrdinalIgnoreCase))
                return "חודשי";

            if (text.Contains("שנתי", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("yearly", StringComparison.OrdinalIgnoreCase))
                return "שנתי";

            return string.Empty;
        }

        // ========================================
        // ✅ Validation משופר
        // ========================================
        private bool ValidateReceipt(ParsedReceipt receipt)
        {
            var isValid = true;

            if (string.IsNullOrWhiteSpace(receipt.MerchantName) || receipt.MerchantName == "לא זוהה")
            {
                receipt.Errors.Add("Merchant name not found");
                isValid = false;
            }

            if (receipt.TotalAmount <= 0)
            {
                receipt.Errors.Add("Invalid amount (must be > 0)");
                isValid = false;
            }

            if (receipt.TransactionDate > DateTime.Today.AddDays(7))
            {
                receipt.Errors.Add("Date is too far in the future");
                isValid = false;
            }

            if (receipt.TransactionDate < DateTime.Today.AddYears(-5))
            {
                receipt.Errors.Add("Date is too far in the past");
                isValid = false;
            }

            return isValid;
        }

        // ========================================
        // Helpers
        // ========================================
        private bool IsNumeric(string text)
        {
            return decimal.TryParse(text.Replace(",", ""), out _);
        }

        private void PrintResults(ParsedReceipt receipt)
        {
            Console.WriteLine("\n[ReceiptParser] ========================================");
            Console.WriteLine("[ReceiptParser] PARSING RESULTS:");
            Console.WriteLine($"[ReceiptParser]   Merchant:     {receipt.MerchantName}");
            Console.WriteLine($"[ReceiptParser]   Amount:       {receipt.TotalAmount:N2} ₪");
            Console.WriteLine($"[ReceiptParser]   Date:         {receipt.TransactionDate:dd/MM/yyyy}");
            Console.WriteLine($"[ReceiptParser]   Last 4:       {receipt.Last4Digits}");
            Console.WriteLine($"[ReceiptParser]   Method:       {receipt.PaymentMethod}");
            Console.WriteLine($"[ReceiptParser]   Installments: {(receipt.HasInstallments ? $"{receipt.InstallmentsCount}x{receipt.InstallmentAmount:N2}" : "No")}");
            Console.WriteLine($"[ReceiptParser]   Recurring:    {receipt.IsRecurring}");
            Console.WriteLine($"[ReceiptParser]   Valid:        {receipt.IsValid}");

            if (receipt.Errors.Any())
            {
                Console.WriteLine($"[ReceiptParser]   Errors:");
                foreach (var error in receipt.Errors)
                {
                    Console.WriteLine($"[ReceiptParser]     - {error}");
                }
            }
            Console.WriteLine("[ReceiptParser] ========================================\n");
        }
    }
}