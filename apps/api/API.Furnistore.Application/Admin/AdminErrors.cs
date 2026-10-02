using API.Furnistore.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace API.Furnistore.Application.Admin
{
    public static class AdminErrors
    {
        public static Error VersionConflict() =>
            Error.Conflict(
                "admin.version_conflict",
                "Alguien más modificó este registro mientras lo editabas. Recarga para ver los cambios."
            );

        public static bool IsUniqueViolation(DbUpdateException exception) =>
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }
}
