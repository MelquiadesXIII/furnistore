using API.Furnistore.API.Extensions;
using API.Furnistore.Application.Admin.Orders;
using API.Furnistore.Shared.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace API.Furnistore.API.Controllers.Admin
{
    [Route("api/admin/orders")]
    public sealed class AdminOrdersController(AdminOrderService orders) : AdminControllerBase
    {
        [HttpGet]
        [ProducesResponseType<PagedResult<AdminOrderSummary>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Search([FromQuery] AdminOrderQuery query, CancellationToken cancellationToken) =>
            (await orders.SearchAsync(query, cancellationToken)).ToActionResult(this);

        [HttpGet("stats")]
        [ProducesResponseType<AdminOrderStats>(StatusCodes.Status200OK)]
        public async Task<IActionResult> Stats(CancellationToken cancellationToken) =>
            (await orders.StatsAsync(cancellationToken)).ToActionResult(this);

        [HttpGet("{id:int}")]
        [ProducesResponseType<AdminOrderResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
            (await orders.GetByIdAsync(id, cancellationToken)).ToActionResult(this);

        [HttpPost("{id:int}/prepare")]
        [ProducesResponseType<AdminOrderResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Prepare(int id, CancellationToken cancellationToken) =>
            (await orders.PrepareAsync(id, User.UserId(), cancellationToken)).ToActionResult(this);

        [HttpPost("{id:int}/ship")]
        [ProducesResponseType<AdminOrderResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Ship(int id, CancellationToken cancellationToken) =>
            (await orders.ShipAsync(id, User.UserId(), cancellationToken)).ToActionResult(this);

        [HttpPost("{id:int}/deliver")]
        [ProducesResponseType<AdminOrderResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Deliver(int id, CancellationToken cancellationToken) =>
            (await orders.DeliverAsync(id, User.UserId(), cancellationToken)).ToActionResult(this);

        [HttpPost("{id:int}/cancel")]
        [ProducesResponseType<AdminOrderResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Cancel(
            int id,
            [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] AdminCancelOrderRequest? request,
            CancellationToken cancellationToken
        ) => (await orders.CancelAsync(id, request ?? new(), User.UserId(), cancellationToken)).ToActionResult(this);
    }
}
