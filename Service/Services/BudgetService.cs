using AutoMapper;
using Common.Dto;
using Repository.Entities;
using Repository.Interfaces;
using Service.Interfaces;

namespace Service.Services
{
    public class BudgetService : IBudgetService
    {
        private readonly IRepository<Budget> _budgetRepository;
        private readonly IRepository<Expense> _expenseRepository;
        private readonly IRepository<Alert> _alertRepository;
        private readonly IRepository<Category> _categoryRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IContext _context;
        private readonly IMapper _mapper;

        public BudgetService(
            IRepository<Budget> budgetRepository,
            IRepository<Expense> expenseRepository,
            IRepository<Alert> alertRepository,
            IRepository<Category> categoryRepository,
            IRepository<User> userRepository,
            IContext context,
            IMapper mapper)
        {
            _budgetRepository = budgetRepository;
            _expenseRepository = expenseRepository;
            _alertRepository = alertRepository;
            _categoryRepository = categoryRepository;
            _userRepository = userRepository;
            _context = context;
            _mapper = mapper;
        }

        // =============================
        // GET ALL
        // =============================

        public async Task<List<BudgetDto>> GetUserBudgets(int userId)
        {
            // 1. שליפת המשתמש כדי לקבל את ההכנסה שלו
            // בהנחה שיש לך UserRepository או דרך לשלוף משתמש
            var user = await _userRepository.GetById(userId);
            if (user == null || user.MonthlyIncome == null)
                return new List<BudgetDto>();

            var income = user.MonthlyIncome.Value;

            // 2. שליפת כל הקטגוריות של המשתמש (כולל המערכתיות)
            var allCategories = await _categoryRepository.Find(c => c.IsSystem || c.UserId == userId);

            // 3. חישוב סך ההוצאות הקבועות (IsFixed)
            // הערה: כאן כדאי שיהיה שדה ב-Category או ב-Budget שמציין מה הסכום הקבוע של אותה קטגוריה
            var fixedExpensesTotal = allCategories
                .Where(c => c.IsFixed)
                .Sum(c => c.BudgetLimit); // או סכום אחר שאתה מגדיר כ"קבוע"

            var disposableIncome = income - fixedExpensesTotal;

            // 4. שליפת התקציבים (האחוזים)
            var budgets = await _budgetRepository.Find(b => b.UserId == userId);

            var dtos = _mapper.Map<List<BudgetDto>>(budgets);

            // 5. חישוב הסכום המחושב לכל שורה
            foreach (var dto in dtos)
            {
                dto.CalculatedAmount = (disposableIncome * dto.Percentage) / 100m;
            }

            return dtos;
        }

        // =============================
        // GET BY ID
        // =============================

        public async Task<BudgetDto?> GetById(int id, int userId)
        {
            var budget = await _budgetRepository.GetById(id);

            if (budget == null || budget.UserId != userId)
                throw new KeyNotFoundException("Budget not found");

            return _mapper.Map<BudgetDto>(budget);
        }


        // =============================
        // ADD — עם validation על אחוזים
        // =============================
        public async Task<BudgetDto> Add(BudgetCreateDto dto, int userId)
        {
            var existing = await _budgetRepository
                .Find(b => b.UserId == userId && b.CategoryId == dto.CategoryId);

            if (existing.Any())
                throw new Exception("Budget for this category already exists");

            // ✅ בדיקה שהסך לא יעבור 100%
            var allBudgets = await _budgetRepository.Find(b => b.UserId == userId);
            var currentTotal = allBudgets.Sum(b => b.Percentage);

            if (currentTotal + dto.Percentage > 100)
                throw new Exception(
                    $"Cannot add budget. Current total: {currentTotal}%, " +
                    $"adding {dto.Percentage}% would exceed 100%");

            var entity = _mapper.Map<Budget>(dto);
            entity.UserId = userId;

            await _budgetRepository.Add(entity);
            await _context.Save();

            return _mapper.Map<BudgetDto>(entity);
        }

        // =============================
        // UPDATE — עם validation על אחוזים
        // =============================
        public async Task<BudgetDto> Update(int id, BudgetUpdateDto dto, int userId)
        {
            var entity = await _budgetRepository.GetById(id);

            if (entity == null || entity.UserId != userId)
                throw new KeyNotFoundException("Budget not found");

            if (dto.Percentage.HasValue)
            {
                // ✅ מחשב את הסך ללא ה-budget הנוכחי
                var allBudgets = await _budgetRepository.Find(b => b.UserId == userId && b.Id != id);
                var otherTotal = allBudgets.Sum(b => b.Percentage);

                if (otherTotal + dto.Percentage.Value > 100)
                    throw new Exception(
                        $"Cannot update. Other budgets total: {otherTotal}%, " +
                        $"setting {dto.Percentage}% would exceed 100%");

                entity.Percentage = dto.Percentage.Value;
            }

            if (dto.CategoryId.HasValue) entity.CategoryId = dto.CategoryId.Value;
            if (dto.AlertEnabled.HasValue) entity.AlertEnabled = dto.AlertEnabled.Value;
            if (dto.AlertThreshold.HasValue) entity.AlertThreshold = dto.AlertThreshold.Value;

            await _context.Save();
            return _mapper.Map<BudgetDto>(entity);
        }

        // =============================
        // DELETE
        // =============================

        public async Task Delete(int id, int userId)
        {
            var entity = await _budgetRepository.GetById(id);

            if (entity == null || entity.UserId != userId)
                throw new KeyNotFoundException("Budget not found");

            await _budgetRepository.Delete(id);

            await _context.Save();
        }


        public async Task CheckBudgetLimit(int userId, int categoryId, IEmailService emailService)
        {
            var user = await _userRepository.GetById(userId);
            if (user == null) return;

            decimal income = user.MonthlyIncome ?? 0;

            var fixedCategories = await _categoryRepository
                .Find(c => c.IsFixed && (c.UserId == userId || c.IsSystem));
            decimal fixedTotal = fixedCategories.Sum(c => c.BudgetLimit);
            decimal disposableIncome = income - fixedTotal;

            // שלוף רק את ה-budget של הקטגוריה הספציפית שהשתנתה
            var budgets = await _budgetRepository
                .Find(b => b.UserId == userId && b.CategoryId == categoryId);

            var budget = budgets.FirstOrDefault();
            if (budget == null || !budget.AlertEnabled) return;

            var expenses = await _expenseRepository
                .Find(e => e.UserId == userId && e.CategoryId == categoryId);

            decimal totalSpent = expenses.Sum(e => e.TotalAmount);

            // ✅ הנוסחה הנכונה
            decimal budgetLimit = (disposableIncome * budget.Percentage) / 100m;
            decimal alertAt = budgetLimit * (budget.AlertThreshold / 100m);

            if (totalSpent >= alertAt)
            {
                // בדוק שלא שלחנו כבר התראה זהה לאחרונה (אופציונלי)
                var alert = new Alert
                {
                    UserId = userId,
                    RelatedEntityType = "Category",
                    RelatedEntityId = categoryId,
                    Message = $"הוצאת {totalSpent:C} מתוך תקציב של {budgetLimit:C} " +
                              $"({budget.AlertThreshold}% מהתקציב).",
                    Title = "חריגה מהתקציב",
                    Type = "BudgetWarning",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                await _alertRepository.Add(alert);
                await _context.Save();

                // ✅ שליחת מייל
                await emailService.SendBudgetAlert(
                    toEmail: user.Email,
                    category: budget.Category?.Name ?? "General", // הגנה מפני Null
                    spent: (double)totalSpent,
                    limit: (double)budgetLimit
                );
            }
        }
    }
}