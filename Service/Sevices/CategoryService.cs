using AutoMapper;
using Common.Dto;
using Repository.Entities;
using Repository.Interfaces;
using Service.Interfaces;

namespace Service.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IRepository<Category> _categoryRepository;
        private readonly IMapper _mapper;

        public CategoryService(IRepository<Category> categoryRepository, IMapper mapper)
        {
            _categoryRepository = categoryRepository;
            _mapper = mapper;
        }

        // ======================
        // GET USER CATEGORIES
        // ======================
        public async Task<List<CategoryDto>> GetUserCategories(int userId)
        {
            var categories = await _categoryRepository
                .Find(c => c.IsSystem || c.UserId == userId);

            return _mapper.Map<List<CategoryDto>>(categories);
        }

        // ======================
        // GET BY ID
        // ======================
        public async Task<CategoryDto?> GetById(int id, int userId)
        {
            var category = await _categoryRepository.GetById(id);

            if (category == null)
                return null;

            if (!category.IsSystem && category.UserId != userId)
                throw new UnauthorizedAccessException();

            return _mapper.Map<CategoryDto>(category);
        }

        // ======================
        // CREATE
        // ======================
        public async Task<CategoryDto> Create(CategoryCreateDto dto, int userId)
        {
            var category = _mapper.Map<Category>(dto);

            category.UserId = userId;
            category.IsSystem = false;

            await _categoryRepository.Add(category);

            return _mapper.Map<CategoryDto>(category);
        }

        // ======================
        // UPDATE
        // ======================
        public async Task<CategoryDto> Update(int id, CategoryUpdateDto dto, int userId)
        {
            var category = await _categoryRepository.GetById(id);

            if (category == null)
                throw new Exception("Category not found");

            if (category.IsSystem)
                throw new Exception("Cannot update system category");

            if (category.UserId != userId)
                throw new UnauthorizedAccessException();

            _mapper.Map(dto, category);

            await _categoryRepository.Update(id, category);

            return _mapper.Map<CategoryDto>(category);
        }

        // ======================
        // DELETE
        // ======================
        public async Task Delete(int id, int userId)
        {
            var category = await _categoryRepository.GetById(id);

            if (category == null)
                throw new Exception("Category not found");

            if (category.IsSystem)
                throw new Exception("Cannot delete system category");

            if (category.UserId != userId)
                throw new UnauthorizedAccessException();

            await _categoryRepository.Delete(id);
        }
    }
}