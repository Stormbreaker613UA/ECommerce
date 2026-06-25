using ECommerce.BLL.Services.Interfaces;
using ECommerce.DAL.DTOs.Category;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoryController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<ActionResult<List<GetCategoryDto>>> GetAllAsync()
    {
        var categories = await _categoryService.GetAllAsync();

        return Ok(categories);
    }

    [HttpGet("root")]
    public async Task<ActionResult<List<GetCategoryDto>>> GetRootCategoriesAsync()
    {
        var categories = await _categoryService.GetRootCategoriesAsync();

        return Ok(categories);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetCategoryDto>> GetByIdAsync(Guid id)
    {
        var category = await _categoryService.GetByIdAsync(id);

        if (category == null)
        {
            return NotFound();
        }

        return Ok(category);
    }

    [HttpGet("{id:guid}/children")]
    public async Task<ActionResult<GetCategoryTreeDto>> GetWithChildrenAsync(Guid id)
    {
        var category = await _categoryService.GetWithChildrenAsync(id);

        if (category == null)
        {
            return NotFound();
        }

        return Ok(category);
    }

    [HttpGet("{parentId:guid}/subcategories")]
    public async Task<ActionResult<List<GetCategoryDto>>> GetSubCategoriesAsync(Guid parentId)
    {
        var categories = await _categoryService.GetSubCategoriesAsync(parentId);

        return Ok(categories);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<GetCategoryDto>> CreateAsync(CreateCategoryDto dto)
    {
        var createdCategory = await _categoryService.CreateAsync(dto);

        return CreatedAtAction(
            nameof(GetByIdAsync),
            new { id = createdCategory.Id },
            createdCategory);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateAsync(Guid id, UpdateCategoryDto dto)
    {
        await _categoryService.UpdateAsync(id, dto);

        return NoContent();
    }

    [HttpPut("{id:guid}/parent")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateParentAsync(Guid id, UpdateCategoryParentDto dto)
    {
        await _categoryService.UpdateParentAsync(id, dto);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        await _categoryService.DeleteAsync(id);

        return NoContent();
    }
}