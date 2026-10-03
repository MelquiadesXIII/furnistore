using API.Furnistore.API.Extensions;
using API.Furnistore.Application.Admin.Products;
using API.Furnistore.Shared.Common;
using Microsoft.AspNetCore.Mvc;

namespace API.Furnistore.API.Controllers.Admin
{
    [Route("api/admin/products")]
    public sealed class AdminProductsController(AdminProductService products) : AdminControllerBase
    {
        [HttpGet]
        [ProducesResponseType<PagedResult<AdminProductResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Search([FromQuery] AdminProductQuery query, CancellationToken cancellationToken) =>
            (await products.SearchAsync(query, cancellationToken)).ToActionResult(this);

        [HttpGet("{id:int}")]
        [ProducesResponseType<AdminProductResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
            (await products.GetByIdAsync(id, cancellationToken)).ToActionResult(this);

        [HttpPost]
        [ProducesResponseType<AdminProductResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create(CreateProductRequest request, CancellationToken cancellationToken)
        {
            var result = await products.CreateAsync(request, User.UserId(), cancellationToken);

            return result.IsSuccess
                ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
                : result.ToActionResult(this);
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType<AdminProductResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Update(int id, UpdateProductRequest request, CancellationToken cancellationToken) =>
            (await products.UpdateAsync(id, request, User.UserId(), cancellationToken)).ToActionResult(this);

        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
            (await products.DeleteAsync(id, User.UserId(), cancellationToken)).ToNoContentResult(this);
    }
}
