using Common.Dto;

namespace Service.Interfaces
{
    public interface ICategoryService
    {
        Task<List<CategoryDto>> GetUserCategories(int userId);

        Task<CategoryDto?> GetById(int id, int userId);

        Task<CategoryDto> Create(CategoryCreateDto dto, int userId);

        Task<CategoryDto> Update(int id, CategoryUpdateDto dto, int userId);

        Task Delete(int id, int userId);
        Task<int> ResolveCategoryId(int userId, string merchantName);
    }
}