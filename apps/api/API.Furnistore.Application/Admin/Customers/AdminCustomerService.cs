using API.Furnistore.Application.Admin.Audit;
using API.Furnistore.Application.Admin.Orders;
using API.Furnistore.Application.Auth;
using API.Furnistore.Application.Clients;
using API.Furnistore.Application.Common;
using API.Furnistore.Data;
using API.Furnistore.Shared;
using API.Furnistore.Shared.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace API.Furnistore.Application.Admin.Customers
{
    public sealed class AdminCustomerService(
        APIFurnistoreContext db,
        AuditLog audit,
        AuditService history,
        AdminOrderService orders,
        TimeProvider clock,
        ILogger<AdminCustomerService> logger
    )
    {
        public const string AdminRole = "Admin";

        private const int RoleChangeLockNamespace = 3005;

        private static readonly SortMap<CustomerRow> Sorts = new SortMap<CustomerRow>()
            .Add("name", row => row.Client.LastName)
            .Add("email", row => row.User.Email)
            .Add("orders", row => row.OrderCount);

        public async Task<Result<PagedResult<AdminCustomerSummary>>> SearchAsync(
            AdminCustomerQuery query,
            CancellationToken cancellationToken
        )
        {
            var now = clock.GetUtcNow();
            var rows = Rows(await AdminRoleIdAsync(cancellationToken));

            rows = query.Filter switch
            {
                CustomerFilter.Admins => rows.Where(row => row.IsAdmin),
                CustomerFilter.Disabled => rows.Where(row => row.User.LockoutEnd >= AccountLock.DisabledUntil),
                CustomerFilter.LockedOut => rows.Where(row =>
                    row.User.LockoutEnd > now && row.User.LockoutEnd < AccountLock.DisabledUntil
                ),
                CustomerFilter.Unconfirmed => rows.Where(row => !row.User.EmailConfirmed),
                _ => rows,
            };

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = LikePattern.Contains(query.Search);
                rows = rows.Where(row =>
                    EF.Functions.ILike(row.Client.FirstName + " " + row.Client.LastName, pattern, LikePattern.Escape)
                    || EF.Functions.ILike(row.User.Email ?? string.Empty, pattern, LikePattern.Escape)
                    || EF.Functions.ILike(row.Client.Phone ?? string.Empty, pattern, LikePattern.Escape)
                );
            }

            var sorted = Sorts.Apply(rows, query.Sort, "name");
            if (!sorted.IsSuccess)
                return Result.Fail<PagedResult<AdminCustomerSummary>>(sorted.Error!);

            var total = await rows.CountAsync(cancellationToken);

            var items = await sorted
                .Value.ThenBy(row => row.Client.FirstName)
                .ThenBy(row => row.Client.ID)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(row => new AdminCustomerSummary(
                    row.Client.ID,
                    row.Client.FirstName,
                    row.Client.LastName,
                    row.User.Email ?? string.Empty,
                    row.Client.Phone,
                    row.User.EmailConfirmed,
                    row.IsAdmin,
                    row.User.LockoutEnd >= AccountLock.DisabledUntil,
                    row.User.LockoutEnd > now && row.User.LockoutEnd < AccountLock.DisabledUntil,
                    row.OrderCount
                ))
                .ToListAsync(cancellationToken);

            return Result.Ok(new PagedResult<AdminCustomerSummary>(items, total, query.Page, query.PageSize));
        }

        public async Task<Result<AdminCustomerResponse>> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            var row = await Rows(await AdminRoleIdAsync(cancellationToken))
                .FirstOrDefaultAsync(row => row.Client.ID == id, cancellationToken);

            if (row is null)
                return Result.Fail<AdminCustomerResponse>(NotFound(id));

            var totalSpent = await db
                .Orders.Where(o => o.ClientId == id && o.Status != OrderStatus.Cancelled)
                .SumAsync(o => (decimal?)o.Total, cancellationToken) ?? 0m;

            var cart = (
                await (
                    from item in db.CartItems.AsNoTracking()
                    join product in db.Products on item.ProductId equals product.Id
                    where item.ClientId == id
                    orderby product.Name
                    select new { product.Id, product.Name, item.Quantity, product.Price }
                ).ToListAsync(cancellationToken)
            )
                .Select(line => new AdminCartLine(line.Id, line.Name, line.Quantity, line.Price, line.Price * line.Quantity))
                .ToList();

            var recent = await orders.SearchAsync(
                new AdminOrderQuery { CustomerId = id, PageSize = 10 },
                cancellationToken
            );

            var client = row.Client;
            var user = row.User;
            var now = clock.GetUtcNow();

            return Result.Ok(
                new AdminCustomerResponse(
                    client.ID,
                    client.FirstName,
                    client.LastName,
                    user.Email ?? string.Empty,
                    client.Phone,
                    client.Street is null || client.City is null || client.Province is null
                        ? null
                        : new ShippingAddress
                        {
                            Street = client.Street,
                            City = client.City,
                            Province = client.Province,
                            DeliveryNotes = client.DeliveryNotes,
                        },
                    client.IsProfileComplete,
                    client.Version,
                    new AdminAccountStatus(
                        user.EmailConfirmed,
                        row.IsAdmin,
                        AccountLock.IsDisabled(user),
                        user.LockoutEnd > now && !AccountLock.IsDisabled(user) ? user.LockoutEnd : null,
                        user.AccessFailedCount
                    ),
                    row.OrderCount,
                    totalSpent,
                    cart,
                    recent.IsSuccess ? recent.Value.Items : [],
                    await history.ForEntityAsync(AuditEntities.Customer, id, cancellationToken)
                )
            );
        }

        public async Task<Result<AdminCustomerResponse>> UpdateAsync(
            int id,
            UpdateCustomerRequest request,
            string actorUserId,
            CancellationToken cancellationToken
        )
        {
            var client = await db.Clients.FirstOrDefaultAsync(c => c.ID == id, cancellationToken);

            if (client is null)
                return Result.Fail<AdminCustomerResponse>(NotFound(id));

            if (client.Version != request.Version)
                return Result.Fail<AdminCustomerResponse>(VersionConflict(id, actorUserId));

            db.Entry(client).Property(c => c.Version).OriginalValue = request.Version;

            var firstName = request.FirstName.Trim();
            var lastName = request.LastName.Trim();
            var phone = request.Phone.Trim();
            var street = request.Address.Street.Trim();
            var city = request.Address.City.Trim();
            var province = request.Address.Province.Trim();
            var notes = TextInput.NullIfBlank(request.Address.DeliveryNotes);

            var changes = new AuditChanges()
                .Track("firstName", client.FirstName, firstName)
                .Track("lastName", client.LastName, lastName)
                .Track("phone", client.Phone, phone)
                .Track("street", client.Street, street)
                .Track("city", client.City, city)
                .Track("province", client.Province, province)
                .Track("deliveryNotes", client.DeliveryNotes, notes);

            if (!changes.IsEmpty)
            {
                client.FirstName = firstName;
                client.LastName = lastName;
                client.Phone = phone;
                client.Street = street;
                client.City = city;
                client.Province = province;
                client.DeliveryNotes = notes;

                audit.Record(
                    actorUserId,
                    AuditActions.CustomerUpdated,
                    AuditEntities.Customer,
                    id,
                    $"Datos de {firstName} {lastName} actualizados",
                    changes
                );

                try
                {
                    await db.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException)
                {
                    return Result.Fail<AdminCustomerResponse>(VersionConflict(id, actorUserId));
                }

                logger.LogInformation(ApiEvents.ClientUpdated, "Client {ClientId} updated by {UserId}", id, actorUserId);
            }

            return await GetByIdAsync(id, cancellationToken);
        }

        public Task<Result<AdminCustomerResponse>> UnlockAsync(int id, string actorUserId, CancellationToken cancellationToken) =>
            ChangeAccountAsync(id, actorUserId, cancellationToken, (target, now) =>
            {
                if (AccountLock.IsDisabled(target.User))
                    return Error.Conflict(
                        "customer.disabled",
                        "La cuenta está desactivada. Reactívala en lugar de desbloquearla."
                    );

                if (!(target.User.LockoutEnd > now) && target.User.AccessFailedCount == 0)
                    return Error.Conflict("customer.not_locked", "La cuenta no está bloqueada.");

                target.User.LockoutEnd = null;
                target.User.AccessFailedCount = 0;
                return new Change(AuditActions.CustomerUnlocked, "desbloqueada", ApiEvents.CustomerUnlocked);
            });

        public Task<Result<AdminCustomerResponse>> DisableAsync(int id, string actorUserId, CancellationToken cancellationToken) =>
            ChangeAccountAsync(id, actorUserId, cancellationToken, (target, _) =>
            {
                if (target.User.Id == actorUserId)
                    return Error.Conflict("customer.self_action", "No puedes desactivar tu propia cuenta.");

                if (target.IsAdmin)
                    return Error.Conflict(
                        "customer.is_admin",
                        "Quítale el rol de administrador antes de desactivar la cuenta."
                    );

                if (AccountLock.IsDisabled(target.User))
                    return Error.Conflict("customer.already_disabled", "La cuenta ya está desactivada.");

                target.User.LockoutEnabled = true;
                target.User.LockoutEnd = AccountLock.DisabledUntil;
                target.RevokeSessions = true;
                return new Change(AuditActions.CustomerDisabled, "desactivada", ApiEvents.CustomerDisabled);
            });

        public Task<Result<AdminCustomerResponse>> EnableAsync(int id, string actorUserId, CancellationToken cancellationToken) =>
            ChangeAccountAsync(id, actorUserId, cancellationToken, (target, _) =>
            {
                if (!AccountLock.IsDisabled(target.User))
                    return Error.Conflict("customer.not_disabled", "La cuenta no está desactivada.");

                target.User.LockoutEnd = null;
                target.User.AccessFailedCount = 0;
                return new Change(AuditActions.CustomerEnabled, "reactivada", ApiEvents.CustomerEnabled);
            });

        public Task<Result<AdminCustomerResponse>> GrantAdminAsync(int id, string actorUserId, CancellationToken cancellationToken) =>
            ChangeAccountAsync(id, actorUserId, cancellationToken, (target, _) =>
            {
                if (target.IsAdmin)
                    return Error.Conflict("customer.already_admin", "Esta cuenta ya es administradora.");

                if (AccountLock.IsDisabled(target.User))
                    return Error.Conflict("customer.disabled", "Reactiva la cuenta antes de darle el rol de administrador.");

                if (!target.User.EmailConfirmed)
                    return Error.Conflict(
                        "customer.unconfirmed",
                        "La cuenta todavía no confirmó su correo, así que no puede ser administradora."
                    );

                target.Role = RoleChange.Grant;
                return new Change(AuditActions.AdminGranted, "ahora es administradora", ApiEvents.AdminRoleGranted);
            });

        public Task<Result<AdminCustomerResponse>> RevokeAdminAsync(int id, string actorUserId, CancellationToken cancellationToken) =>
            ChangeAccountAsync(id, actorUserId, cancellationToken, (target, _) =>
            {
                if (!target.IsAdmin)
                    return Error.Conflict("customer.not_admin", "Esta cuenta no es administradora.");

                if (target.User.Id == actorUserId)
                    return Error.Conflict("customer.self_action", "No puedes quitarte el rol de administrador a ti mismo.");

                if (target.AdminCount <= 1)
                    return Error.Conflict("customer.last_admin", "No se puede quitar el rol al último administrador.");

                target.Role = RoleChange.Revoke;
                return new Change(AuditActions.AdminRevoked, "ya no es administradora", ApiEvents.AdminRoleRevoked);
            });

        public async Task<int?> FindIdByEmailAsync(string email, CancellationToken cancellationToken)
        {
            var normalized = email.Trim().ToUpperInvariant();

            return await (
                from client in db.Clients
                join user in db.Users on client.UserId equals user.Id
                where user.NormalizedEmail == normalized
                select (int?)client.ID
            ).FirstOrDefaultAsync(cancellationToken);
        }

        private async Task<Result<AdminCustomerResponse>> ChangeAccountAsync(
            int id,
            string actorUserId,
            CancellationToken cancellationToken,
            Func<AccountTarget, DateTimeOffset, Decision> decide
        )
        {
            var strategy = db.Database.CreateExecutionStrategy();

            var outcome = await strategy.ExecuteAsync(async () =>
            {
                db.ChangeTracker.Clear();
                await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

                await db.Database.ExecuteSqlAsync(
                    $"SELECT pg_advisory_xact_lock({RoleChangeLockNamespace}, 0)",
                    cancellationToken
                );

                var adminRoleId = await AdminRoleIdAsync(cancellationToken);

                var found = await (
                    from client in db.Clients
                    join user in db.Users on client.UserId equals user.Id
                    where client.ID == id
                    select new { Client = client, User = user }
                ).FirstOrDefaultAsync(cancellationToken);

                if (found is null)
                    return (Error?)NotFound(id);

                var target = new AccountTarget
                {
                    User = found.User,
                    IsAdmin = adminRoleId is not null
                        && await db.UserRoles.AnyAsync(
                            ur => ur.UserId == found.User.Id && ur.RoleId == adminRoleId,
                            cancellationToken
                        ),
                    AdminCount = adminRoleId is null
                        ? 0
                        : await db.UserRoles.CountAsync(ur => ur.RoleId == adminRoleId, cancellationToken),
                };

                var decision = decide(target, clock.GetUtcNow());

                if (decision.Error is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    logger.LogWarning(
                        ApiEvents.CustomerActionRejected,
                        "Account action on client {ClientId} by {UserId} rejected: {Code}",
                        id,
                        actorUserId,
                        decision.Error.Code
                    );
                    return decision.Error;
                }

                var change = decision.Change!;

                if (target.Role != RoleChange.None)
                {
                    if (adminRoleId is null)
                    {
                        var role = new IdentityRole(AdminRole) { NormalizedName = AdminRole.ToUpperInvariant() };
                        db.Roles.Add(role);
                        adminRoleId = role.Id;
                    }

                    if (target.Role == RoleChange.Grant)
                        db.UserRoles.Add(new IdentityUserRole<string> { UserId = found.User.Id, RoleId = adminRoleId });
                    else
                        await db
                            .UserRoles.Where(ur => ur.UserId == found.User.Id && ur.RoleId == adminRoleId)
                            .ExecuteDeleteAsync(cancellationToken);
                }

                if (target.RevokeSessions)
                    await db
                        .RefreshTokens.Where(t => t.UserId == found.User.Id && !t.IsRevoked && !t.IsUsed)
                        .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsRevoked, true), cancellationToken);

                found.User.SecurityStamp = Guid.NewGuid().ToString();

                audit.Record(
                    actorUserId,
                    change.AuditAction,
                    AuditEntities.Customer,
                    id,
                    $"Cuenta de {found.Client.FirstName} {found.Client.LastName} {change.Description}"
                );

                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                logger.LogInformation(
                    change.Event,
                    "Account of client {ClientId} {Action} by {UserId}",
                    id,
                    change.AuditAction,
                    actorUserId
                );

                return null;
            });

            return outcome is null
                ? await GetByIdAsync(id, cancellationToken)
                : Result.Fail<AdminCustomerResponse>(outcome);
        }

        private Task<string?> AdminRoleIdAsync(CancellationToken cancellationToken) =>
            db.Roles
                .Where(r => r.NormalizedName == AdminRole.ToUpperInvariant())
                .Select(r => r.Id)
                .FirstOrDefaultAsync(cancellationToken);

        private IQueryable<CustomerRow> Rows(string? adminRoleId) =>
            from client in db.Clients.AsNoTracking()
            join user in db.Users.AsNoTracking() on client.UserId equals user.Id
            select new CustomerRow
            {
                Client = client,
                User = user,
                IsAdmin = adminRoleId != null && db.UserRoles.Any(ur => ur.UserId == user.Id && ur.RoleId == adminRoleId),
                OrderCount = db.Orders.Count(o => o.ClientId == client.ID),
            };

        private Error NotFound(int id)
        {
            logger.LogWarning(ApiEvents.ClientNotFound, "Client {ClientId} not found", id);
            return Error.NotFound("customer.not_found", $"No existe el cliente {id}.");
        }

        private Error VersionConflict(int id, string actorUserId)
        {
            logger.LogWarning(
                ApiEvents.VersionConflict,
                "Client {ClientId} update by {UserId} rejected: stale version",
                id,
                actorUserId
            );
            return AdminErrors.VersionConflict();
        }

        private enum RoleChange
        {
            None,
            Grant,
            Revoke,
        }

        private sealed record Change(string AuditAction, string Description, EventId Event);

        private sealed class Decision
        {
            public Error? Error { get; private init; }

            public Change? Change { get; private init; }

            public static implicit operator Decision(Error error) => new() { Error = error };

            public static implicit operator Decision(Change change) => new() { Change = change };
        }

        private sealed class AccountTarget
        {
            public required IdentityUser User { get; init; }

            public required bool IsAdmin { get; init; }

            public required int AdminCount { get; init; }

            public RoleChange Role { get; set; }

            public bool RevokeSessions { get; set; }
        }

        private sealed class CustomerRow
        {
            public required Client Client { get; init; }

            public required IdentityUser User { get; init; }

            public required bool IsAdmin { get; init; }

            public required int OrderCount { get; init; }
        }
    }
}
