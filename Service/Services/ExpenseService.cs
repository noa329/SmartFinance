using AutoMapper;
using Common.Dto;
using Repository.Entities;
using Repository.Interfaces;
using Repository.Repositories;
using Service.Interfaces;

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
            var expenses = await _expenseRepository.Find(e => e.UserId == userId);

            return _mapper.Map<List<ExpenseDto>>(expenses);
        }

        //public async Task AddExpense(createExpenseDto dto, int userId)
        //{

        //    int categoryId;
        //    string source;

        //    if (dto.CategoryId != 0)
        //    {
        //        categoryId = dto.CategoryId;
        //        source = "Manual";
        //    }
        //    else
        //    {
        //         categoryId = await _categoryService
        //            .ResolveCategoryId(userId, dto.MerchantName);

        //       source = "Mail";
        //    }
        //    var expense = new Expense
        //    {
        //        UserId = userId,
        //        MerchantName = dto.MerchantName,
        //        TotalAmount = dto.Amount,
        //        TransactionDate = dto.Date,
        //        CategoryId = categoryId,
        //        EventId = dto.EventId,
        //        InstallmentsCount = 1,
        //        InstallmentAmount = dto.Amount,
        //        FirstInstallmentDate = dto.Date,
        //        Source = source,
        //        OriginalText = dto.Description
        //    };

        //    await _expenseRepository.Add(expense);

        //    await CheckBudgetAlert(expense);
        //}
        // Services/ExpenseService.cs
        public async Task AddExpense(createExpenseDto dto, int userId)
        {
            Console.WriteLine($"\n[ExpenseService] Adding expense for User: {userId}");

            int categoryId;
            string source;

            if (dto.CategoryId != 0)
            {
                categoryId = dto.CategoryId;
                source = "Manual";
            }
            else
            {
                categoryId = await _categoryService.ResolveCategoryId(userId, dto.MerchantName);
                source = "Mail";
            }
            if (dto.InstallmentsCount > 1)
            {
                dto.StartDate = dto.Date;
                dto.EndDate = dto.Date.AddMonths(dto.InstallmentsCount - 1);

                //dto.IsRecurring = true;
                dto.DayOfMonth = dto.Date.Day;
            }

            // ✅ תיקון: טיפול נכון בתשלומים
            var expense = new Expense
            {
                UserId = userId,
                MerchantName = dto.MerchantName,
                TotalAmount = dto.Amount,
                TransactionDate = dto.Date,
                CategoryId = categoryId,
                EventId = dto.EventId,

                // ✅ תשלומים
                InstallmentsCount = dto.InstallmentsCount > 0 ? dto.InstallmentsCount : 1,
                InstallmentAmount = dto.InstallmentAmount > 0 ? dto.InstallmentAmount : dto.Amount,
                FirstInstallmentDate = dto.FirstInstallmentDate ?? dto.Date,


                // ✅ הוצאה קבועה
                IsRecurring = dto.IsRecurring,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                DayOfMonth = dto.DayOfMonth,

                // Metadata
                Source = source,
                OriginalText = dto.Description ?? "",
                //StartDate = DateTime.UtcNow
            };

            Console.WriteLine($"[ExpenseService] Expense details:");
            Console.WriteLine($"  Merchant: {expense.MerchantName}");
            Console.WriteLine($"  Amount: {expense.TotalAmount:N2}");
            Console.WriteLine($"  Category: {categoryId}");
            Console.WriteLine($"  Installments: {expense.InstallmentsCount}x{expense.InstallmentAmount:N2}");

            await _expenseRepository.Add(expense);

            Console.WriteLine($"[ExpenseService] ✅ Expense added successfully!");

            await CheckBudgetAlert(expense);
        }
        private async Task CheckBudgetAlert(Expense expense)
        {
            var category = await _categoryRepository.GetById(expense.CategoryId);
            if (category == null || category.BudgetLimit <= 0)
                return;

            var user = await _userRepository.GetById(expense.UserId);
            if (user == null || !user.IsAlertEnabled)
                return;

            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;
            var expenses = await _expenseRepository.Find(e =>
                e.UserId == expense.UserId &&
                e.CategoryId == expense.CategoryId &&
                e.TransactionDate.Month == currentMonth &&
                e.TransactionDate.Year == currentYear);

            decimal spent = expenses.Sum(e => e.TotalAmount);

            decimal limit = category.BudgetLimit;

            double percentageLimit = user.AlertPercentage ?? 80;

            double currentPercentage = (double)(spent / limit) * 100;

            if (currentPercentage < percentageLimit)
                return;

            var alert = new Alert
            {
                UserId = user.Id,
                Type = "Budget",
                Title = "Budget Warning",
                Message = $"You used {currentPercentage:F0}% of {category.Name}",
                RelatedEntityType = "Category",
                RelatedEntityId = category.Id
            };

            await _alertRepository.Add(alert);

            //await _emailService.SendBudgetAlert(
            //    user.Email,
            //    category.Name,
            //    (double)spent,
            //    (double)limit);
        }
        public async Task DeleteExpense(int id, int userId)
        {
            var expense = await _expenseRepository.GetById(id);

            if (expense == null || expense.UserId != userId)
                throw new Exception("Expense not found");

            await _expenseRepository.Delete(id);
        }
    }
}