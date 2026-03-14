using AutoMapper;
using Common.Dto;
using Repository.Entities;
using Repository.Interfaces;
using Service.Interfaces;

namespace Service.Sevices
{
    public class BudgetService : IBudgetService
    {
        private readonly IRepository<Budget> _budgetRepository;
        private readonly IRepository<Expense> _expenseRepository;
        private readonly IRepository<Alert> _alertRepository;
        private readonly IContext _context;
        private readonly IMapper _mapper;

        public BudgetService(
            IRepository<Budget> budgetRepository,
            IRepository<Expense> expenseRepository,
            IRepository<Alert> alertRepository,
            IContext context,
            IMapper mapper)
        {
            _budgetRepository = budgetRepository;
            _expenseRepository = expenseRepository;
            _alertRepository = alertRepository;
            _context = context;
            _mapper = mapper;
        }

        // =============================
        // GET ALL
        // =============================

        public async Task<List<BudgetDto>> GetUserBudgets(int userId)
        {
            var budgets = await _budgetRepository.Find(b => b.UserId == userId);

            return _mapper.Map<List<BudgetDto>>(budgets);
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
        // ADD
        // =============================

        public async Task<BudgetDto> Add(BudgetCreateDto dto, int userId)
        {
            var entity = _mapper.Map<Budget>(dto);

            // קובע את המשתמש מהטוקן
            entity.UserId = userId;

            await _budgetRepository.Add(entity);
            await _context.Save();

            return _mapper.Map<BudgetDto>(entity);
        }


        // =============================
        // UPDATE
        // =============================

        public async Task<BudgetDto> Update(int id, BudgetUpdateDto dto, int userId)
        {
            var entity = await _budgetRepository.GetById(id);

            if (entity == null || entity.UserId != userId)
                throw new KeyNotFoundException("Budget not found");

            if (dto.CategoryId.HasValue)
                entity.CategoryId = dto.CategoryId.Value;

            if (dto.Percentage.HasValue)
                entity.Percentage = dto.Percentage.Value;

            if (dto.AlertEnabled.HasValue)
                entity.AlertEnabled = dto.AlertEnabled.Value;

            if (dto.AlertThreshold.HasValue)
                entity.AlertThreshold = dto.AlertThreshold.Value;

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


        // =============================
        // ALERT ENGINE ⭐
        // =============================

        public async Task CheckBudgetLimit(int userId)
        {
            var budgets = await _budgetRepository.Find(b => b.UserId == userId);

            var expenses = await _expenseRepository.Find(e => e.UserId == userId);

            foreach (var budget in budgets)
            {
                var categoryExpenseSum = expenses
                    .Where(e => e.CategoryId == budget.CategoryId)
                    .Sum(e => e.TotalAmount);

                var limit = (budget.AlertThreshold * budget.Percentage) / 100m;

                if (categoryExpenseSum >= limit && budget.AlertEnabled)
                {
                    await _alertRepository.Add(new Alert
                    {
                        UserId = userId,
                        RelatedEntityType = "Category",
                        RelatedEntityId = budget.CategoryId,
                        Message = $"Warning! Budget category {budget.CategoryId} is near limit.",
                        Title = "Budget Alert",
                        Type = "BudgetWarning",
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    });
                }
            }

            await _context.Save();
        }
    }
}