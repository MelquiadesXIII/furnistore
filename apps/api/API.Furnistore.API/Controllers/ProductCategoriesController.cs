using API.Furnistore.API.Extensions;
using API.Furnistore.Application.ProductCategories;
using API.Furnistore.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Furnistore.API.Controllers
{
    [ApiController]
    [Route("api/product-categories")]
    public sealed class ProductCategoriesController(ProductCategoryService categories)
        : ControllerBase
    {
        // El catálogo se muestra antes de iniciar sesión, así que las lecturas son públicas.
        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType<PagedResult<ProductCategoryResponse>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> Search(
            [FromQuery] ProductCategoryQuery query,
            CancellationToken cancellationToken
        ) => (await categories.SearchAsync(query, cancellationToken)).ToActionResult(this);

        [AllowAnonymous]
        [HttpGet("{id:int}")]
        [ProducesResponseType<ProductCategoryResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
            (await categories.GetByIdAsync(id, cancellationToken)).ToActionResult(this);

        [Authorize]
        [HttpPost]
        [ProducesResponseType<ProductCategoryResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create(
            CreateProductCategoryRequest request,
            CancellationToken cancellationToken
        )
        {
            var result = await categories.CreateAsync(request, User.UserId(), cancellationToken);

            return result.IsSuccess
                ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
                : result.ToActionResult(this);
        }

        [Authorize]
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Update(
            int id,
            UpdateProductCategoryRequest request,
            CancellationToken cancellationToken
        ) =>
            (await categories.UpdateAsync(id, request, User.UserId(), cancellationToken))
                .ToNoContentResult(this);

        [Authorize]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
            (await categories.DeleteAsync(id, User.UserId(), cancellationToken))
                .ToNoContentResult(this);
    }
}
