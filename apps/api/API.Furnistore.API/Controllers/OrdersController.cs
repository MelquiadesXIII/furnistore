using API.Furnistore.API.Extensions;
using API.Furnistore.Application.Orders;
using API.Furnistore.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Furnistore.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/orders")]
    public sealed class OrdersController(OrderService orders) : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType<PagedResult<OrderResponse>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> Search(
            [FromQuery] OrderQuery query,
            CancellationToken cancellationToken
        ) => (await orders.SearchAsync(query, cancellationToken)).ToActionResult(this);

        [HttpGet("{id:int}")]
        [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
            (await orders.GetByIdAsync(id, cancellationToken)).ToActionResult(this);

        [HttpPost]
        [ProducesResponseType<OrderResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create(
            CreateOrderRequest request,
            CancellationToken cancellationToken
        )
        {
            var result = await orders.CreateAsync(request, User.UserId(), cancellationToken);

            return result.IsSuccess
                ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
                : result.ToActionResult(this);
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Update(
            int id,
            UpdateOrderRequest request,
            CancellationToken cancellationToken
        ) =>
            (await orders.UpdateAsync(id, request, User.UserId(), cancellationToken))
                .ToNoContentResult(this);

        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
            (await orders.DeleteAsync(id, User.UserId(), cancellationToken))
                .ToNoContentResult(this);
    }
}
