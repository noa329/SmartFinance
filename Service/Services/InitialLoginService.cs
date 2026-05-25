// ============================================================
//  Service/Services/InitialLoginService.cs
// ============================================================
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

        // מיפוי סטטי לצורך זיהוי אוטומטי של קטגוריה לפי שם ההוצאה
        private static readonly Dictionary<string, string> _expenseCategoryMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "משכנתא",         "דיור"      },
            { "שכר דירה",       "דיור"      },
            { "ביטוח רכב",      "תחבורה"    },
            { "ביטוח בריאות",   "בריאות"    },
            { "ביטוח חיים",     "ביטוח"     },
            { "אינטרנט",        "תקשורת"    },
            { "טלפון",          "תקשורת"    },
            { "ספורט",          "בריאות"    },
            { "נטפליקס",        "בידור"     },
        };

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

        public async Task InitializeUserCategories(int userId)
        {
            Console.WriteLine($"[InitCategories] Initializing categories for User: {userId}");
            var defaultCategories = GetDefaultCategories();
            var existingCategories = await _categoryRepository.Find(c => c.UserId == userId);

            foreach (var category in defaultCategories)
            {
                if (existingCategories.Any(c => c.Name == category.Name))
                    continue;

                var userCategory = new Category
                {
                    Name = category.Name,
                    IsFixed = category.IsFixed,
                    IsSystem = true,
                    UserId = userId
                };

                await _categoryRepository.Add(userCategory);
            }
        }

        // ─── מתודה חדשה ומאוחדת לשמירת הוצאות קבועות מה-Setup ───
        public async Task SaveFixedExpenses(int userId, List<FixedExpenseDto> expenses)
        {
            // 1. מחיקת הוצאות setup קודמות כדי לאפשר עריכה וחזרה על השלב במידת הצורך
            var existingSetupExpenses = await _expenseRepository.Find(e => e.UserId == userId && e.Source == "setup" && e.IsRecurring);
            foreach (var oldExpense in existingSetupExpenses)
            {
                await _expenseRepository.Delete(oldExpense.Id);
            }

            // 2. ריצה על ההוצאות החדשות ושמירתן ב-DB
            foreach (var e in expenses.Where(exp => exp.Amount > 0))
            {
                int categoryId = e.CategoryId ?? await GetDefaultCategoryId(userId, e.Name);

                var expenseEntity = new Expense
                {
                    UserId = userId,
                    MerchantName = e.Name,
                    TotalAmount = e.Amount,
                    InstallmentAmount = e.Amount,
                    InstallmentsCount = 1,
                    TransactionDate = DateTime.Today,
                    FirstInstallmentDate = DateTime.Today,
                    IsRecurring = true,
                    StartDate = DateTime.Today,
                    EndDate = null, // פתוח - רץ קבוע
                    DayOfMonth = DateTime.Today.Day,
                    Source = "setup",
                    OriginalText = $"הוצאה קבועה: {e.Name}",
                    CategoryId = categoryId
                };

                await _expenseRepository.Add(expenseEntity);
            }
        }

        // הלוגיקה הפנימית למציאת מזהה קטגוריה לפי שם ההוצאה באמצעות ה-Repository הגנרי
        private async Task<int> GetDefaultCategoryId(int userId, string expenseName)
        {
            if (_expenseCategoryMap.TryGetValue(expenseName, out var categoryName))
            {
                var categories = await _categoryRepository.Find(c =>
                    (c.UserId == userId || c.IsSystem) && c.Name == categoryName);

                var cat = categories.FirstOrDefault();
                if (cat != null) return cat.Id;
            }

            // ברירת מחדל במידה ולא נמצאה התאמה במילון - קטגוריית "אחר"
            var defaultCategories = await _categoryRepository.Find(c =>
                (c.UserId == userId || c.IsSystem) && c.Name == "אחר");

            var defaultCat = defaultCategories.FirstOrDefault();
            return defaultCat?.Id ?? 1; // החזרת מזהה 1 כהגנה אחרונה
        }

        public async Task<List<BudgetDto>> CalculateBudget(int userId)
        {
            var user = await _userRepository.GetById(userId);
            if (user == null)
                throw new Exception("User not found");

            var now = DateTime.UtcNow;
            var expenses = await _expenseRepository.Find(e => e.UserId == userId);
            decimal totalMonthlyFixed = 0;

            // 1. חישוב סך ההוצאות הקבועות שכבר נשמרו בשלב 2
            foreach (var e in expenses)
            {
                if (e.InstallmentsCount > 1)
                {
                    var monthsPassed = ((now.Year - e.FirstInstallmentDate.Year) * 12) + now.Month - e.FirstInstallmentDate.Month;
                    if (monthsPassed >= 0 && monthsPassed < e.InstallmentsCount)
                    {
                        totalMonthlyFixed += e.InstallmentAmount;
                    }
                }
                else if (e.IsRecurring && e.StartDate.HasValue)
                {
                    bool withinPeriod = e.StartDate.Value <= now && (e.EndDate == null || e.EndDate >= now);
                    if (withinPeriod)
                    {
                        totalMonthlyFixed += e.TotalAmount;
                    }
                }
            }

            // חישוב ההכנסה הפנויה
            var remaining = user.MonthlyIncome - totalMonthlyFixed;
            if (remaining < 0)
                throw new Exception("The fixed costs are higher than the income");

            // 2. 🔥 התיקון הקריטי: בדיקה ויצירת תקציבים ראשוניים אם לא קיימים
            var budgets = await _budgetRepository.Find(b => b.UserId == userId);

            if (!budgets.Any())
            {
                // מושכים את הקטגוריות של המשתמש שהן משתנות (לא קבועות/IsFixed), כי עליהן בונים תקציב גמיש
                var userCategories = await _categoryRepository.Find(c => c.UserId == userId && !c.IsFixed);

                if (!userCategories.Any())
                {
                    // הגנה למקרה חירום - אם אין עדיין קטגוריות מותאמות, ניקח את כל הקטגוריות הזמינות
                    userCategories = await _categoryRepository.Find(c => c.UserId == userId || c.IsSystem);
                }

                // חלוקת אחוזים ראשונית שווה בין הקטגוריות הגמישות
                int categoryCount = userCategories.Count(c => !c.IsFixed) > 0 ? userCategories.Count(c => !c.IsFixed) : userCategories.Count;
                int defaultPercentage = 100 / categoryCount;
                int remainder = 100 % categoryCount; // שארית חלוקה כדי להגיע בדיוק ל-100%

                var newBudgets = new List<Budget>();
                var flexibleCategories = userCategories.Where(c => !c.IsFixed).ToList();
                if (!flexibleCategories.Any()) flexibleCategories = userCategories.ToList();

                for (int i = 0; i < flexibleCategories.Count; i++)
                {
                    var cat = flexibleCategories[i];
                    // הוספת השארית לפריט האחרון כדי להבטיח סך הכל מדויק של 100%
                    int finalPercent = (i == flexibleCategories.Count - 1) ? defaultPercentage + remainder : defaultPercentage;

                    var newBudget = new Budget
                    {
                        UserId = userId,
                        CategoryId = cat.Id,
                        Percentage = finalPercent,
                        CalculatedAmount = (remaining * finalPercent) / 100,
                        AlertEnabled = true,
                        AlertThreshold = 90
                    };

                    await _budgetRepository.Add(newBudget);
                    newBudgets.Add(newBudget);
                }

                // מעדכנים את המשתנה המקומי ברשימה החדשה שיצרנו
                budgets = newBudgets;
            }
            else
            {
                // 3. אם התקציבים כבר היו קיימים ב-DB, רק נעדכן את ה-CalculatedAmount לפי ההכנסה הפנויה הנוכחית
                foreach (var budget in budgets)
                {
                    var amount = (remaining * budget.Percentage) / 100;
                    budget.CalculatedAmount = amount;
                    await _budgetRepository.Update(budget.Id, budget);
                }
            }

            return _mapper.Map<List<BudgetDto>>(budgets);
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
                    bool withinPeriod = e.StartDate.Value <= lastOfMonth && (!e.EndDate.HasValue || e.EndDate.Value >= firstOfMonth);
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