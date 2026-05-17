using Common.Dto;

namespace Service.Interfaces
{
    public interface IExpenseService
    {
        Task<List<ExpenseDto>> GetUserExpenses(int userId);

        Task AddExpense(createExpenseDto dto, int userId);

        Task DeleteExpense(int id, int userId);
        Task UpdateExpenseEvent(int expenseId, int? eventId, int userId);
    }
}