using Microsoft.AspNetCore.Identity;

namespace API.Furnistore.Application.Auth
{
    public static class AccountLock
    {
        public static readonly DateTimeOffset DisabledUntil = new(9999, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public static bool IsDisabled(IdentityUser user) => user.LockoutEnd >= DisabledUntil;
    }
}
