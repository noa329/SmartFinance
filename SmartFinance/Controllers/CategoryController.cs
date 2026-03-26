using Common.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using Service.Services;
using System.Security.Claims;

namespace SmartFinance.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }

        // ======================
        // GET ALL
        // ======================
        [HttpGet]
        public async Task<ActionResult<List<CategoryDto>>> Get()
        {
            var userId = GetUserId();

            var categories = await _categoryService.GetUserCategories(userId);

            return Ok(categories);
        }

        // ======================
        // GET BY ID
        // ======================
        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryDto>> GetById(int id)
        {
            var userId = GetUserId();

            var category = await _categoryService.GetById(id, userId);

            if (category == null)
                return NotFound();

            return Ok(category);
        }

        // ======================
        // CREATE
        // ======================
        [HttpPost]
        public async Task<ActionResult<CategoryDto>> Create(CategoryCreateDto dto)
        {
            var userId = GetUserId();

            var category = await _categoryService.Create(dto, userId);

            return Ok(category);
        }

        // ======================
        // UPDATE
        // ======================
        [HttpPut("{id}")]
        public async Task<ActionResult<CategoryDto>> Update(int id, CategoryUpdateDto dto)
        {
            var userId = GetUserId();

            var category = await _categoryService.Update(id, dto, userId);

            return Ok(category);
        }

        // ======================
        // DELETE
        // ======================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();

            await _categoryService.Delete(id, userId);

            return NoContent();
        }
    }
}