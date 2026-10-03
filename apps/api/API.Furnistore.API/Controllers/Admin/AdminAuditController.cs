using API.Furnistore.API.Extensions;
using API.Furnistore.Application.Admin.Audit;
using API.Furnistore.Shared.Common;
using Microsoft.AspNetCore.Mvc;

namespace API.Furnistore.API.Controllers.Admin
{
    [Route("api/admin/audit")]
    public sealed class AdminAuditController(AuditService audit) : AdminControllerBase
    {
        [HttpGet]
        [ProducesResponseType<PagedResult<AuditEntryResponse>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> Search([FromQuery] AuditQuery query, CancellationToken cancellationToken) =>
            (await audit.SearchAsync(query, cancellationToken)).ToActionResult(this);
    }
}
