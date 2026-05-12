using Common.Dto;

namespace Service.Interfaces
{
    public interface IBudgetService
    {
        Task<List<BudgetDto>> GetUserBudgets(int userId);

        Task<BudgetDto?> GetById(int id, int userId);

        Task<BudgetDto> Add(BudgetCreateDto dto, int userId);

        Task<BudgetDto> Update(int id, BudgetUpdateDto dto, int userId);

        Task Delete(int id, int userId);

        //Task CheckBudgetLimit(int userId);
        Task CheckBudgetLimit(int userId, int categoryId, IEmailService emailService);
    }
}
