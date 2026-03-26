using Common.Dto;

namespace Service.Interfaces
{
    public interface IInitialLoginService
    {
        Task SetInitialIncome(int userId, decimal amount, bool isYearly);

        Task InitializeUserCategories(int userId);

        Task AddFixedExpenses(int userId, List<createExpenseDto> expenses);

        Task SetCategoryBudgets(int userId, List<BudgetCreateDto> budgets);
        Task<List<BudgetDto>> CalculateBudget(int userId);
        Task<List<BudgetDto>> CalculateBudgetMonthly(int userId, DateTime? targetMonth = null);
    }
}