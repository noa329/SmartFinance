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

        // ============================
        // Income Setup
        // ============================

        public async Task SetInitialIncome(int userId, decimal monthlyIncome)
        {
            var user = await _userRepository.GetById(userId);

            if (user == null)
                throw new Exception("User not found");

            user.MonthlyIncome = monthlyIncome;

            await _userRepository.Update(userId, user);
        }

        // ============================
        // Categories Initialization
        // ============================

        public async Task InitializeUserCategories(int userId)
        {
            var systemCategories = await _categoryRepository
                .Find(c => c.IsSystem);

            foreach (var category in systemCategories)
            {
                var userCategory = new Category
                {
                    Name = category.Name,
                    IsFixed = category.IsFixed,
                    IsSystem = false,
                    UserId = userId
                };

                await _categoryRepository.Add(userCategory);
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
    }
}