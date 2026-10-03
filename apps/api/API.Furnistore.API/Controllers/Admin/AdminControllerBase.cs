using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Furnistore.API.Controllers.Admin
{
    [ApiController]
    [Authorize(Roles = AdminRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public abstract class AdminControllerBase : ControllerBase;

    public static class AdminRoles
    {
        public const string Admin = "Admin";
    }
}
