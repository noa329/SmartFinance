using Service.Models;
using Common.Dto;
using Service.Interfaces;

namespace Service.Services
{

    public class ReceiptProcessorService : IReceiptProcessorService
    {
        private readonly IReceiptParserService _parserService;
        private readonly IExpenseService _expenseService;
        private readonly ICategoryService _categoryService;

        public ReceiptProcessorService(
            IReceiptParserService parserService,
            IExpenseService expenseService,
            ICategoryService categoryService)
        {
            _parserService = parserService;
            _expenseService = expenseService;
            _categoryService = categoryService;
        }

        public async Task<int> ProcessReceipt(string receiptText, int userId)
        {
            Console.WriteLine("\n[ReceiptProcessor] ========================================");
            Console.WriteLine($"[ReceiptProcessor] Processing receipt for User: {userId}");
            Console.WriteLine("[ReceiptProcessor] ========================================");

            // 1. Parse טקסט
            var parsedReceipt = await _parserService.ParseReceiptText(receiptText);

            if (!parsedReceipt.IsValid)
            {
                Console.WriteLine($"[ReceiptProcessor] ❌ Invalid receipt:");
                foreach (var error in parsedReceipt.Errors)
                {
                    Console.WriteLine($"[ReceiptProcessor]   - {error}");
                }
                throw new Exception($"Invalid receipt: {string.Join(", ", parsedReceipt.Errors)}");
            }

            // 2. המרה ל-DTO
            var expenseDto = new createExpenseDto
            {
                MerchantName = parsedReceipt.MerchantName,
                Amount = parsedReceipt.TotalAmount,
                Date = parsedReceipt.TransactionDate,
                Description = parsedReceipt.OriginalText,
                CategoryId = 0, // ✅ יסווג אוטומטית ב-ExpenseService

                // Installments
                InstallmentsCount = parsedReceipt.InstallmentsCount,
                InstallmentAmount = parsedReceipt.HasInstallments
                    ? parsedReceipt.InstallmentAmount
                    : parsedReceipt.TotalAmount,
                FirstInstallmentDate = parsedReceipt.FirstInstallmentDate ?? parsedReceipt.TransactionDate,

                // Recurring
                IsRecurring = parsedReceipt.IsRecurring,
                StartDate = parsedReceipt.IsRecurring ? parsedReceipt.TransactionDate : null,
                DayOfMonth = parsedReceipt.IsRecurring ? parsedReceipt.TransactionDate.Day : null,
            };

            Console.WriteLine("\n[ReceiptProcessor] Creating Expense DTO:");
            Console.WriteLine($"[ReceiptProcessor]   Merchant: {expenseDto.MerchantName}");
            Console.WriteLine($"[ReceiptProcessor]   Amount: {expenseDto.Amount:N2}");
            Console.WriteLine($"[ReceiptProcessor]   Installments: {expenseDto.InstallmentsCount}x{expenseDto.InstallmentAmount:N2}");
            Console.WriteLine($"[ReceiptProcessor]   Recurring: {expenseDto.IsRecurring}");

            // 3. יצירת Expense
            await _expenseService.AddExpense(expenseDto, userId);

            Console.WriteLine("[ReceiptProcessor] ✅ Expense created successfully!");
            Console.WriteLine("[ReceiptProcessor] ========================================\n");

            return 1; // Success
        }
    }
}
