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

        [Authorize]
        [HttpPost]
        [ProducesResponseType<ProductResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create(
            CreateProductRequest request,
            CancellationToken cancellationToken
        )
        {
            var result = await products.CreateAsync(request, User.UserId(), cancellationToken);

            return result.IsSuccess
                ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
                : result.ToActionResult(this);
        }

        [Authorize]
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(
            int id,
            UpdateProductRequest request,
            CancellationToken cancellationToken
        ) =>
            (await products.UpdateAsync(id, request, User.UserId(), cancellationToken))
                .ToNoContentResult(this);

        [Authorize]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
            (await products.DeleteAsync(id, User.UserId(), cancellationToken))
                .ToNoContentResult(this);
    }
}
