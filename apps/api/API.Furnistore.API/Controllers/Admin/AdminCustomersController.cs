using API.Furnistore.API.Extensions;
using API.Furnistore.Application.Admin.Customers;
using API.Furnistore.Shared.Common;
using Microsoft.AspNetCore.Mvc;

namespace API.Furnistore.API.Controllers.Admin
{
    [Route("api/admin/customers")]
    public sealed class AdminCustomersController(AdminCustomerService customers) : AdminControllerBase
    {
        [HttpGet]
        [ProducesResponseType<PagedResult<AdminCustomerSummary>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Search([FromQuery] AdminCustomerQuery query, CancellationToken cancellationToken) =>
            (await customers.SearchAsync(query, cancellationToken)).ToActionResult(this);

        [HttpGet("{id:int}")]
        [ProducesResponseType<AdminCustomerResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
            (await customers.GetByIdAsync(id, cancellationToken)).ToActionResult(this);

        [HttpPut("{id:int}")]
        [ProducesResponseType<AdminCustomerResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Update(int id, UpdateCustomerRequest request, CancellationToken cancellationToken) =>
            (await customers.UpdateAsync(id, request, User.UserId(), cancellationToken)).ToActionResult(this);

        [HttpPost("{id:int}/unlock")]
        [ProducesResponseType<AdminCustomerResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Unlock(int id, CancellationToken cancellationToken) =>
            (await customers.UnlockAsync(id, User.UserId(), cancellationToken)).ToActionResult(this);

        [HttpPost("{id:int}/disable")]
        [ProducesResponseType<AdminCustomerResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Disable(int id, CancellationToken cancellationToken) =>
            (await customers.DisableAsync(id, User.UserId(), cancellationToken)).ToActionResult(this);

        [HttpPost("{id:int}/enable")]
        [ProducesResponseType<AdminCustomerResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Enable(int id, CancellationToken cancellationToken) =>
            (await customers.EnableAsync(id, User.UserId(), cancellationToken)).ToActionResult(this);

        [HttpPost("{id:int}/admin-role")]
        [ProducesResponseType<AdminCustomerResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> GrantAdmin(int id, CancellationToken cancellationToken) =>
            (await customers.GrantAdminAsync(id, User.UserId(), cancellationToken)).ToActionResult(this);

        [HttpDelete("{id:int}/admin-role")]
        [ProducesResponseType<AdminCustomerResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> RevokeAdmin(int id, CancellationToken cancellationToken) =>
            (await customers.RevokeAdminAsync(id, User.UserId(), cancellationToken)).ToActionResult(this);
    }
}
