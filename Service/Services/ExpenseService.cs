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
        private readonly EmailService _emailService;
        private readonly IMapper _mapper;


        public ExpenseService(
            IRepository<Expense> expenseRepository,
            IRepository<Category> categoryRepository,
            IRepository<User> userRepository,
            IRepository<Alert> alertRepository,
            EmailService emailService,
            IMapper mapper)
        {
            _expenseRepository = expenseRepository;
            _categoryRepository = categoryRepository;
            _userRepository = userRepository;
            _alertRepository = alertRepository;
            _emailService = emailService;
            _mapper = mapper;
        }

        public async Task<List<ExpenseDto>> GetUserExpenses(int userId)
        {
            var expenses = await _expenseRepository.Find(e => e.UserId == userId);

            return _mapper.Map<List<ExpenseDto>>(expenses);
        }

        public async Task AddExpense(createExpenseDto dto, int userId)
        {
            var expense = new Expense
            {
                UserId = userId,
                MerchantName = dto.Description,
                TotalAmount = dto.Amount,
                TransactionDate = dto.Date,
                CategoryId = dto.CategoryId,
                EventId = dto.EventId,
                InstallmentsCount = 1,
                InstallmentAmount = dto.Amount,
                FirstInstallmentDate = dto.Date,
                Source = "Manual",
                OriginalText = dto.Description
            };

            await _expenseRepository.Add(expense);

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

            var expenses = await _expenseRepository.Find(e =>
                e.UserId == expense.UserId &&
                e.CategoryId == expense.CategoryId);

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

            await _emailService.SendBudgetAlert(
                user.Email,
                category.Name,
                (double)spent,
                (double)limit);
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