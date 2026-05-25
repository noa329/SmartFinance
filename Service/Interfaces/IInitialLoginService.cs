using Common.Dto;

namespace Service.Interfaces
{
    public interface IInitialLoginService
    {
        Task SetInitialIncome(int userId, decimal amount, bool isYearly);
        Task InitializeUserCategories(int userId);
        Task<List<BudgetDto>> CalculateBudget(int userId);
        Task<List<BudgetDto>> CalculateBudgetMonthly(int userId, DateTime? targetMonth);
        Task SaveFixedExpenses(int userId, List<FixedExpenseDto> expenses);
    }
}