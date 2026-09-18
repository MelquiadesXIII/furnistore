using API.Furnistore.API.Extensions;
using API.Furnistore.Application.Clients;
using API.Furnistore.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Furnistore.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/clients")]
    public sealed class ClientsController(ClientService clients) : ControllerBase
    {
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType<PagedResult<ClientResponse>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> Search(
            [FromQuery] ClientQuery query,
            CancellationToken cancellationToken
        ) => (await clients.SearchAsync(query, cancellationToken)).ToActionResult(this);

        [HttpGet("me")]
        [ProducesResponseType<ClientResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMe(CancellationToken cancellationToken) =>
            (await clients.GetMeAsync(User.UserId(), cancellationToken)).ToActionResult(this);

        [HttpPut("me")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> UpdateMe(
            UpdateClientRequest request,
            CancellationToken cancellationToken
        ) =>
            (await clients.UpdateMeAsync(User.UserId(), request, cancellationToken))
                .ToNoContentResult(this);

        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType<ClientResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
            (await clients.GetByIdAsync(id, cancellationToken)).ToActionResult(this);

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType<ClientResponse>(StatusCodes.Status201Created)]
        public async Task<IActionResult> Create(
            CreateClientRequest request,
            CancellationToken cancellationToken
        )
        {
            var result = await clients.CreateAsync(request, User.UserId(), cancellationToken);

            return result.IsSuccess
                ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
                : result.ToActionResult(this);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Update(
            int id,
            UpdateClientRequest request,
            CancellationToken cancellationToken
        ) =>
            (await clients.UpdateAsync(id, request, User.UserId(), cancellationToken))
                .ToNoContentResult(this);

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
            (await clients.DeleteAsync(id, User.UserId(), cancellationToken))
                .ToNoContentResult(this);
    }
}
