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
        [HttpGet("me")]
        [ProducesResponseType<ClientResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMe(CancellationToken cancellationToken) =>
            (await clients.GetMeAsync(User.UserId(), cancellationToken)).ToActionResult(this);

        [HttpPut("me")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateMe(
            UpdateClientRequest request,
            CancellationToken cancellationToken
        ) =>
            (await clients.UpdateMeAsync(User.UserId(), request, cancellationToken))
                .ToNoContentResult(this);
    }
}
