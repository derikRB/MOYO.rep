using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Interfaces;

namespace Sego_and__Bux.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductTypeController : ControllerBase
    {
        private readonly IProductTypeService _productTypeService;
        public ProductTypeController(IProductTypeService productTypeService) => _productTypeService = productTypeService;

        // 🔓 Anyone authenticated (including customers) can view product types
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll() => Ok(await _productTypeService.GetAllProductTypesAsync());

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> Get(int id) => Ok(await _productTypeService.GetProductTypeByIdAsync(id));

        // 🔐 Restricted to Employee and Admin
        [HttpPost]
        [Authorize(Roles = "Admin,Manager,Employee")]
        public async Task<IActionResult> Create(ProductTypeDto dto) => Ok(await _productTypeService.AddProductTypeAsync(dto));

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Manager,Employee")]
        public async Task<IActionResult> Update(int id, ProductTypeDto dto) => Ok(await _productTypeService.UpdateProductTypeAsync(id, dto));

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Manager,Employee")]
        public async Task<IActionResult> Delete(int id) => Ok(await _productTypeService.DeleteProductTypeAsync(id));
    }
}
