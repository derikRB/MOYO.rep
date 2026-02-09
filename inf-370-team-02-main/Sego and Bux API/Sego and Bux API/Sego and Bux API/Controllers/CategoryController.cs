using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Interfaces;
using Sego_and__Bux.Models;

namespace Sego_and__Bux.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Manager,Employee")] // Restricts everything by default
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        public CategoryController(ICategoryService categoryService) => _categoryService = categoryService;

        // ----- FIXED: AllowAnonymous for GETs -----
        [HttpGet]
        [AllowAnonymous] // Anyone can fetch categories!
        public async Task<IActionResult> GetAll() => Ok(await _categoryService.GetAllCategoriesAsync());

        [HttpGet("{id}")]
        [AllowAnonymous] // Anyone can fetch category by ID!
        public async Task<IActionResult> Get(int id) => Ok(await _categoryService.GetCategoryByIdAsync(id));

        // --- Auth required for below ---
        [HttpPost]
        public async Task<IActionResult> Create(CategoryDto dto) => Ok(await _categoryService.AddCategoryAsync(dto));

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, CategoryDto dto) => Ok(await _categoryService.UpdateCategoryAsync(id, dto));

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id) => Ok(await _categoryService.DeleteCategoryAsync(id));
    }
}
