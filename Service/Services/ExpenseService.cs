using AutoMapper;
using Common.Dto;
using Repository.Entities;
using Repository.Interfaces;
using Repository.Repositories;
using Service.Interfaces;
using System.Text;

namespace Service.Services
{
    public class ExpenseService : IExpenseService
    {
        private readonly IRepository<Expense> _expenseRepository;
        private readonly IRepository<Category> _categoryRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Alert> _alertRepository;
        private readonly ICategoryService _categoryService;
        private readonly EmailService _emailService;
        private readonly IMapper _mapper;

        public ExpenseService(
            IRepository<Expense> expenseRepository,
            IRepository<Category> categoryRepository,
            IRepository<User> userRepository,
            IRepository<Alert> alertRepository,
            EmailService emailService,
            IMapper mapper,
            ICategoryService categoryService)
        {
            _expenseRepository = expenseRepository;
            _categoryRepository = categoryRepository;
            _userRepository = userRepository;
            _alertRepository = alertRepository;
            _emailService = emailService;
            _mapper = mapper;
            _categoryService = categoryService;
        }

        public async Task<List<ExpenseDto>> GetUserExpenses(int userId)
        {
            var expenses = await ((ExpenseRepository)_expenseRepository)
                .FindWithIncludes(e => e.UserId == userId);
            return _mapper.Map<List<ExpenseDto>>(expenses);
        }

        public async Task AddExpense(createExpenseDto dto, int userId, string? messageId = null)
        {
            Console.WriteLine($"[ExpenseService] Adding expense for User {userId}: {dto.MerchantName}");

            // קטגוריה: ידנית או אוטומטית דרך AI
            int categoryId = dto.CategoryId != 0
                ? dto.CategoryId
                : await _categoryService.ResolveCategoryId(userId, dto.MerchantName);

            // תשלומים: חישוב תאריכים אוטומטי
            if (dto.InstallmentsCount > 1)
            {
                dto.StartDate ??= dto.Date;
                dto.EndDate ??= dto.Date.AddMonths(dto.InstallmentsCount - 1);
                dto.DayOfMonth ??= dto.Date.Day;
            }

            var expense = new Expense
            {
                UserId = userId,
                MerchantName = dto.MerchantName,
                TotalAmount = dto.Amount,
                TransactionDate = dto.Date == default ? DateTime.UtcNow : dto.Date,
                CategoryId = categoryId,
                EventId = dto.EventId,
                GmailMessageId = messageId,

                InstallmentsCount = dto.InstallmentsCount > 0 ? dto.InstallmentsCount : 1,
                InstallmentAmount = dto.InstallmentAmount > 0 ? dto.InstallmentAmount : dto.Amount,
                FirstInstallmentDate = dto.FirstInstallmentDate ?? dto.Date,

                IsRecurring = dto.IsRecurring,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                DayOfMonth = dto.DayOfMonth,

                Source = messageId == null ? "Manual" : "Mail",
                OriginalText = dto.Description ?? string.Empty
            };

            await _expenseRepository.Add(expense);
            Console.WriteLine($"[ExpenseService] ✅ Saved: {expense.MerchantName} {expense.TotalAmount:N2}₪");

            await CheckBudgetAlert(expense);
        }

        public async Task DeleteExpense(int id, int userId)
        {
            var expense = await _expenseRepository.GetById(id);
            if (expense == null || expense.UserId != userId)
                throw new KeyNotFoundException("Expense not found or unauthorized.");

            await _expenseRepository.Delete(id);
        }

        public async Task UpdateExpenseEvent(int expenseId, int? eventId, int userId)
        {
            var expense = await _expenseRepository.GetById(expenseId);
            if (expense == null || expense.UserId != userId)
                throw new KeyNotFoundException("Expense not found or unauthorized.");

            expense.EventId = eventId;
            await _expenseRepository.Update(expenseId, expense);
        }

        // ─────────────────────────────────────────
        // בדיקת תקציב ויצירת התראה אם חורגים
        // ─────────────────────────────────────────
        private async Task CheckBudgetAlert(Expense expense)
        {
            var category = await _categoryRepository.GetById(expense.CategoryId);
            if (category == null || category.BudgetLimit <= 0) return;

            var user = await _userRepository.GetById(expense.UserId);
            if (user == null || !user.IsAlertEnabled) return;

            // סכום ההוצאות בחודש הנוכחי לאותה קטגוריה
            var now = DateTime.Now;
            var monthExpenses = await _expenseRepository.Find(e =>
                e.UserId == expense.UserId &&
                e.CategoryId == expense.CategoryId &&
                e.TransactionDate.Month == now.Month &&
                e.TransactionDate.Year == now.Year);

            decimal spent = monthExpenses.Sum(e => e.TotalAmount);
            decimal limit = category.BudgetLimit;
            double threshold = user.AlertPercentage ?? 80;
            double pct = (double)(spent / limit) * 100;

            if (pct < threshold) return;

            // שמירת ההתראה ב-DB (תישלח ב-Sync היומי)
            var alert = new Alert
            {
                UserId = user.Id,
                Type = "Budget",
                Title = "Budget Warning",
                Message = $"ניצלת {pct:F0}% מתקציב {category.Name} ({spent:N2}/{limit:N2} ₪)",
                RelatedEntityType = "Category",
                RelatedEntityId = category.Id,
                IsSent = false
            };

            await _alertRepository.Add(alert);
            Console.WriteLine($"[ExpenseService] ⚠️ Budget alert created: {pct:F0}% of {category.Name}");
        }
    }
}