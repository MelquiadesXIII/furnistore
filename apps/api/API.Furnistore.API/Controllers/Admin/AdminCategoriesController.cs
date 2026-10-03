using API.Furnistore.API.Extensions;
using API.Furnistore.Application.Admin.Categories;
using API.Furnistore.Shared.Common;
using Microsoft.AspNetCore.Mvc;

namespace API.Furnistore.API.Controllers.Admin
{
    [Route("api/admin/product-categories")]
    public sealed class AdminCategoriesController(AdminCategoryService categories) : AdminControllerBase
    {
        [HttpGet]
        [ProducesResponseType<PagedResult<AdminCategoryResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Search([FromQuery] AdminCategoryQuery query, CancellationToken cancellationToken) =>
            (await categories.SearchAsync(query, cancellationToken)).ToActionResult(this);

        [HttpPost]
        [ProducesResponseType<AdminCategoryResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create(SaveCategoryRequest request, CancellationToken cancellationToken) =>
            (await categories.CreateAsync(request, User.UserId(), cancellationToken)).ToActionResult(this);

        [HttpPut("{id:int}")]
        [ProducesResponseType<AdminCategoryResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Update(int id, SaveCategoryRequest request, CancellationToken cancellationToken) =>
            (await categories.UpdateAsync(id, request, User.UserId(), cancellationToken)).ToActionResult(this);

        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
            (await categories.DeleteAsync(id, User.UserId(), cancellationToken)).ToNoContentResult(this);
    }
}
