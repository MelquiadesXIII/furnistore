using API.Furnistore.API.Extensions;
using API.Furnistore.Application.Orders;
using API.Furnistore.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

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
        ) => (await orders.SearchAsync(query, User.UserId(), cancellationToken)).ToActionResult(this);

        [HttpGet("{id:int}")]
        [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
            (await orders.GetByIdAsync(id, User.UserId(), cancellationToken)).ToActionResult(this);

        [HttpPost("checkout")]
        [ProducesResponseType<OrderResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Checkout(
            CheckoutRequest request,
            CancellationToken cancellationToken
        )
        {
            var result = await orders.CheckoutAsync(request, User.UserId(), cancellationToken);

            return result.IsSuccess
                ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
                : result.ToActionResult(this);
        }

        [HttpPost("{id:int}/cancel")]
        [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Cancel(
            int id,
            [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CancelOrderRequest? request,
            CancellationToken cancellationToken
        ) =>
            (await orders.CancelAsync(id, request ?? new(), User.UserId(), cancellationToken))
                .ToActionResult(this);
    }
}
