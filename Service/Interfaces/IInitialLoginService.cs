using Common.Dto;

namespace Service.Interfaces
{
    public interface IInitialLoginService
    {
        Task SetInitialIncome(int userId, decimal monthlyIncome);

        Task InitializeUserCategories(int userId);

        Task AddFixedExpenses(int userId, List<createExpenseDto> expenses);

        Task SetCategoryBudgets(int userId, List<BudgetCreateDto> budgets);
    }
}