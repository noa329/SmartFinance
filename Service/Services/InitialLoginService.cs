using AutoMapper;
using Common.Dto;
using Repository.Entities;
using Repository.Interfaces;
using Service.Interfaces;

namespace Service.Services
{
    public class InitialLoginService : IInitialLoginService
    {
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Category> _categoryRepository;
        private readonly IRepository<Expense> _expenseRepository;
        private readonly IRepository<Budget> _budgetRepository;

        private readonly IMapper _mapper;

        public InitialLoginService(
            IRepository<User> userRepository,
            IRepository<Category> categoryRepository,
            IRepository<Expense> expenseRepository,
            IRepository<Budget> budgetRepository,
            IMapper mapper)
        {
            _userRepository = userRepository;
            _categoryRepository = categoryRepository;
            _expenseRepository = expenseRepository;
            _budgetRepository = budgetRepository;
            _mapper = mapper;
        }

        private List<Category> GetDefaultCategories()
        {
            return new List<Category>
    {

        new Category { Name = "אחר", IsFixed = true ,IsSystem=true},
        new Category { Name = "קניות", IsFixed = true ,IsSystem=true},
        new Category { Name = "שכר דירה", IsFixed = true ,IsSystem=true},
        new Category { Name = "חשבונות", IsFixed = true ,IsSystem=true},
        new Category { Name = "אוכל", IsFixed = false ,IsSystem=true},
        new Category { Name = "בריאות", IsFixed = false, IsSystem = true },
        new Category { Name = "בילויים", IsFixed = false ,IsSystem=true},
        new Category { Name = "תחבורה", IsFixed = false ,IsSystem=true}
    };
        }

        // ============================
        // Income Setup
        // ============================

        //public async Task SetInitialIncome(int userId, decimal monthlyIncome)
        //{
        //    var user = await _userRepository.GetById(userId);

        //    if (user == null)
        //        throw new Exception("User not found");

        //    user.MonthlyIncome = monthlyIncome;

        //    await _userRepository.Update(userId, user);
        //}
        public async Task SetInitialIncome(int userId, decimal amount, bool isYearly)
        {
            var user = await _userRepository.GetById(userId);

            if (user == null)
                throw new Exception("User not found");

            if (isYearly)
            {
                user.MonthlyIncome = amount / 12;
                user.YearlyIncome = amount;
            }
            else
            {
                user.MonthlyIncome = amount;
                user.YearlyIncome = amount * 12;
            }


            await _userRepository.Update(userId, user);
        }

        // ============================
        // Categories Initialization
        // ============================

        //public async Task InitializeUserCategories(int userId)
        //{
        //    var defaultCategories = GetDefaultCategories();
        //    var existingCategories = await _categoryRepository.GetAll(); 

        //    foreach (var category in defaultCategories)
        //    {
        //        if (existingCategories.Any(c => c.UserId == userId && c.Name == category.Name))
        //            continue;

        //        var userCategory = new Category
        //        {
        //            Name = category.Name,
        //            IsFixed = category.IsFixed,
        //            IsSystem = true,
        //            UserId = userId
        //        };

        //        await _categoryRepository.Add(userCategory);
        //    }
        //}
        public async Task InitializeUserCategories(int userId)
        {
            Console.WriteLine($"[InitCategories] Initializing categories for User: {userId}");

            var defaultCategories = GetDefaultCategories();

            // ✅ תיקון: בדיקה רק לקטגוריות של המשתמש הזה
            var existingCategories = await _categoryRepository.Find(c => c.UserId == userId);

            Console.WriteLine($"[InitCategories] User already has {existingCategories.Count()} categories");

            foreach (var category in defaultCategories)
            {
                // בדיקה אם כבר קיים
                if (existingCategories.Any(c => c.Name == category.Name))
                {
                    Console.WriteLine($"[InitCategories] ⏭️ Skipping '{category.Name}' (already exists)");
                    continue;
                }

                var userCategory = new Category
                {
                    Name = category.Name,
                    IsFixed = category.IsFixed,
                    IsSystem = true,  // ✅ זו קטגוריית ברירת מחדל
                    UserId = userId   // ✅ חובה! שייכת למשתמש הספציפי
                };

                await _categoryRepository.Add(userCategory);
                Console.WriteLine($"[InitCategories] ✅ Created category '{category.Name}' for User {userId}");
            }
        }

        // ============================
        // Fixed Expenses
        // ============================

        public async Task AddFixedExpenses(int userId, List<createExpenseDto> expenses)
        {
            foreach (var dto in expenses)
            {
                var expense = _mapper.Map<Expense>(dto);

                expense.UserId = userId;
                expense.Source = "InitialSetup";

                await _expenseRepository.Add(expense);
            }
        }

        // ============================
        // Category Budgets
        // ============================

        public async Task SetCategoryBudgets(int userId, List<BudgetCreateDto> budgets)
        {
            foreach (var budgetDto in budgets)
            {
                var budget = new Budget
                {
                    UserId = userId,
                    CategoryId = budgetDto.CategoryId,
                    Percentage = budgetDto.Percentage,
                    AlertEnabled = true,
                    AlertThreshold = 80
                };

                await _budgetRepository.Add(budget);
            }
        }

        public async Task<List<BudgetDto>> CalculateBudget(int userId)
        {
            var user = await _userRepository.GetById(userId);
            if (user == null)
                throw new Exception("User not found");

            var now = DateTime.UtcNow;

            var expenses = await _expenseRepository.Find(e => e.UserId == userId);

            decimal totalMonthlyFixed = 0;

            foreach (var e in expenses)
            {
                if (e.InstallmentsCount > 1)
                {
                    var monthsPassed = ((now.Year - e.FirstInstallmentDate.Year) * 12) +
                                        now.Month - e.FirstInstallmentDate.Month;

                    if (monthsPassed >= 0 && monthsPassed < e.InstallmentsCount)
                    {
                        totalMonthlyFixed += e.InstallmentAmount;
                    }
                }
                else if (e.IsRecurring && e.StartDate.HasValue)
                {
                    bool withinPeriod = e.StartDate.Value <= now &&
                                         (e.EndDate == null || e.EndDate >= now);

                    if (withinPeriod)
                    {
                        totalMonthlyFixed += e.TotalAmount;
                    }
                }
            }

            var remaining = user.MonthlyIncome - totalMonthlyFixed;

            if (remaining < 0)
                throw new Exception("The fixed costs are higher than the income");

            var budgets = await _budgetRepository.Find(b => b.UserId == userId);

            foreach (var budget in budgets)
            {
                var amount = (remaining * budget.Percentage) / 100;
                budget.CalculatedAmount = amount;

                await _budgetRepository.Update(budget.Id, budget);
            }

            var budgetDtos = _mapper.Map<List<BudgetDto>>(budgets);
            return budgetDtos;
        }
        public async Task<List<BudgetDto>> CalculateBudgetMonthly(int userId, DateTime? targetMonth = null)
        {
            var user = await _userRepository.GetById(userId);
            if (user == null)
                throw new Exception("User not found");

            var month = targetMonth ?? DateTime.UtcNow;
            var firstOfMonth = new DateTime(month.Year, month.Month, 1);
            var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);

            var expenses = await _expenseRepository.Find(e => e.UserId == userId);

            decimal totalFixedThisMonth = 0;

            foreach (var e in expenses)
            {

                if (e.InstallmentsCount > 1)
                {
                    for (int i = 0; i < e.InstallmentsCount; i++)
                    {
                        var installmentDate = e.FirstInstallmentDate.AddMonths(i);

                        if (installmentDate >= firstOfMonth && installmentDate <= lastOfMonth)
                        {
                            totalFixedThisMonth += e.InstallmentAmount;
                            break; 
                        }
                    }
                }

                else if (e.IsRecurring && e.StartDate.HasValue)
                {
                    bool withinPeriod = e.StartDate.Value <= lastOfMonth &&
                                        (!e.EndDate.HasValue || e.EndDate.Value >= firstOfMonth);

                    if (withinPeriod)
                    {
                        if (e.DayOfMonth.HasValue)
                        {
                            var expenseDate = new DateTime(month.Year, month.Month, e.DayOfMonth.Value);
                            if (expenseDate >= firstOfMonth && expenseDate <= lastOfMonth)
                            {
                                totalFixedThisMonth += e.TotalAmount;
                            }
                        }
                        else
                        {
                            totalFixedThisMonth += e.TotalAmount;
                        }
                    }
                }
            }

            var remaining = user.MonthlyIncome - totalFixedThisMonth;
            if (remaining < 0)
                throw new Exception("The fixed costs are higher than the income");

            var budgets = await _budgetRepository.Find(b => b.UserId == userId);
            foreach (var budget in budgets)
            {
                var amount = (remaining * budget.Percentage) / 100;
                budget.CalculatedAmount = amount;

                await _budgetRepository.Update(budget.Id, budget);
            }

            return _mapper.Map<List<BudgetDto>>(budgets);
        }
    }
}