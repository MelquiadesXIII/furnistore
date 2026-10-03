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
    }
}
