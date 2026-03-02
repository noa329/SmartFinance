using Common.Dto;

namespace Service.Interfaces
{
    public interface IBudgetService
    {
        Task<List<BudgetDto>> GetAll();
        Task<BudgetDto?> GetById(int id);
        Task<BudgetDto> Add(BudgetCreateDto dto);
        Task<BudgetDto> Update(int id, BudgetUpdateDto dto);
        Task Delete(int id);
    }
}
