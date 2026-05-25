using AutoMapper;
using Common.Dto;
using Repository.Entities;
using Repository.Interfaces;
using Service.Interfaces;

namespace Service.Services
{
    public class BudgetService : IBudgetService
    {
        private readonly IRepository<Budget> _budgetRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<Category> _categoryRepo;
        private readonly IRepository<Expense> _expenseRepo;
        private readonly IRepository<Alert> _alertRepo;
        private readonly IMapper _mapper;

        public BudgetService(
            IRepository<Budget> budgetRepo,
            IRepository<User> userRepo,
            IRepository<Category> categoryRepo,
            IRepository<Expense> expenseRepo,
            IRepository<Alert> alertRepo,
            IMapper mapper)
        {
            _budgetRepo = budgetRepo;
            _userRepo = userRepo;
            _categoryRepo = categoryRepo;
            _expenseRepo = expenseRepo;
            _alertRepo = alertRepo;
            _mapper = mapper;
        }

        public async Task<List<BudgetDto>> GetUserBudgets(int userId)
        {
            var user = await _userRepo.GetById(userId);
            if (user?.MonthlyIncome == null) return new List<BudgetDto>();

            decimal disposableIncome = await CalculateDisposableIncome(userId, user.MonthlyIncome.Value);
            var budgets = await _budgetRepo.Find(b => b.UserId == userId);

            var dtos = _mapper.Map<List<BudgetDto>>(budgets);
            dtos.ForEach(d => d.CalculatedAmount = (disposableIncome * d.Percentage) / 100m);

            return dtos;
        }

        public async Task<BudgetDto> Add(BudgetCreateDto dto, int userId)
        {
            var existing = await _budgetRepo.Find(b => b.UserId == userId && b.CategoryId == dto.CategoryId);
            if (existing.Any()) throw new InvalidOperationException("Budget for this category already exists");

            await ValidateTotalPercentage(userId, dto.Percentage);

            var entity = _mapper.Map<Budget>(dto);
            entity.UserId = userId;

            await _budgetRepo.Add(entity);
            return _mapper.Map<BudgetDto>(entity);
        }

        public async Task<BudgetDto> Update(int id, BudgetUpdateDto dto, int userId)
        {
            var existingEntity = await _budgetRepo.GetById(id);
            if (existingEntity == null || existingEntity.UserId != userId)
                throw new KeyNotFoundException("Budget not found");

            if (dto.Percentage.HasValue)
            {
                await ValidateTotalPercentage(userId, dto.Percentage.Value, id);
                existingEntity.Percentage = dto.Percentage.Value;
            }

            if (dto.CategoryId.HasValue) existingEntity.CategoryId = dto.CategoryId.Value;
            existingEntity.AlertEnabled = dto.AlertEnabled ?? existingEntity.AlertEnabled;
            existingEntity.AlertThreshold = dto.AlertThreshold ?? existingEntity.AlertThreshold;

            await _budgetRepo.Update(id, existingEntity);
            return _mapper.Map<BudgetDto>(existingEntity);
        }

        public async Task Delete(int id, int userId)
        {
            var entity = await _budgetRepo.GetById(id);
            if (entity == null || entity.UserId != userId) throw new KeyNotFoundException();
            await _budgetRepo.Delete(id);
        }

        public async Task CheckBudgetLimit(int userId, int categoryId, IEmailService emailService)
        {
            var user = await _userRepo.GetById(userId);
            var budgets = await _budgetRepo.Find(b => b.UserId == userId && b.CategoryId == categoryId);
            var budget = budgets.FirstOrDefault();

            // בדיקת בטיחות שהכל קיים והתראות מאופשרות
            if (user == null || user.Email == null || budget == null || !budget.AlertEnabled) return;

            decimal disposableIncome = await CalculateDisposableIncome(userId, user.MonthlyIncome ?? 0);
            decimal budgetLimit = (disposableIncome * budget.Percentage) / 100m;

            // שימוש ב-TransactionDate כפי שמופיע בישות Expense שלך
            var expenses = await _expenseRepo.Find(e =>
                e.UserId == userId &&
                e.CategoryId == categoryId &&
                e.TransactionDate.Month == DateTime.Now.Month &&
                e.TransactionDate.Year == DateTime.Now.Year);

            decimal totalSpent = expenses.Sum(e => e.TotalAmount);

            // בדיקה האם עברנו את סף ההתראה (למשל 80%)
            if (totalSpent >= budgetLimit * (budget.AlertThreshold / 100m))
            {
                await CreateAndSendAlert(user, budget, totalSpent, budgetLimit, emailService);
            }
        }

        private async Task CreateAndSendAlert(User user, Budget budget, decimal spent, decimal limit, IEmailService emailService)
        {
            // טיפול בשם הקטגוריה עבור הודעות
            string categoryName = budget.Category?.Name ?? "כללי";

            var alert = new Alert
            {
                UserId = user.Id,
                Title = "התראת תקציב",
                Message = $"ניצלת {spent:N0}₪ מתוך תקציב של {limit:N0}₪ בקטגוריית {categoryName}",
                Type = "BudgetWarning",
                CreatedAt = DateTime.UtcNow
            };

            await _alertRepo.Add(alert);

            // שליחת המייל באופן אסינכרוני
            try
            {
                await emailService.SendBudgetAlert(user.Email!, categoryName, (double)spent, (double)limit);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email: {ex.Message}");
            }
        }

        public async Task<BudgetDto?> GetById(int id, int userId)
        {
            var budget = await _budgetRepo.GetById(id);
            if (budget == null || budget.UserId != userId)
                throw new KeyNotFoundException("Budget not found");
            return _mapper.Map<BudgetDto>(budget);
        }

        private async Task<decimal> CalculateDisposableIncome(int userId, decimal income)
        {
            var now = DateTime.UtcNow;
            // שליפת כל ההוצאות של המשתמש שהן קבועות/רצות
            var expenses = await _expenseRepo.Find(e => e.UserId == userId);
            decimal totalFixed = 0;

            foreach (var e in expenses)
            {
                if (e.InstallmentsCount > 1)
                {
                    var monthsPassed = ((now.Year - e.FirstInstallmentDate.Year) * 12) + now.Month - e.FirstInstallmentDate.Month;
                    if (monthsPassed >= 0 && monthsPassed < e.InstallmentsCount)
                    {
                        totalFixed += e.InstallmentAmount;
                    }
                }
                else if (e.IsRecurring && e.StartDate.HasValue)
                {
                    bool withinPeriod = e.StartDate.Value <= now && (e.EndDate == null || e.EndDate >= now);
                    if (withinPeriod)
                    {
                        totalFixed += e.TotalAmount;
                    }
                }
            }

            return income - totalFixed;
        }

        private async Task ValidateTotalPercentage(int userId, int newPercentage, int? excludeId = null)
        {
            var allBudgets = await _budgetRepo.Find(b => b.UserId == userId && b.Id != excludeId);
            if (allBudgets.Sum(b => b.Percentage) + newPercentage > 100)
                throw new ArgumentException("Total budget percentage cannot exceed 100%");
        }
    }
}