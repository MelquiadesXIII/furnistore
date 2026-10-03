using API.Furnistore.API.Extensions;
using API.Furnistore.Application.Products;
using API.Furnistore.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Furnistore.API.Controllers
{
    [ApiController]
    [Route("api/products")]
    public sealed class ProductsController(ProductService products) : ControllerBase
    {
        // El catálogo se muestra antes de iniciar sesión, así que las lecturas son públicas.
        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType<PagedResult<ProductResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Search(
            [FromQuery] ProductQuery query,
            CancellationToken cancellationToken
        ) => (await products.SearchAsync(query, cancellationToken)).ToActionResult(this);

        [AllowAnonymous]
        [HttpGet("{id:int}")]
        [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
            (await products.GetByIdAsync(id, cancellationToken)).ToActionResult(this);
    }
}
