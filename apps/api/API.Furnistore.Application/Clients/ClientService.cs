using API.Furnistore.Application.Common;
using API.Furnistore.Data;
using API.Furnistore.Shared;
using API.Furnistore.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace API.Furnistore.Application.Clients
{
    public sealed class ClientService(APIFurnistoreContext db, ILogger<ClientService> logger)
    {
        public async Task<Result<PagedResult<ClientResponse>>> SearchAsync(
            ClientQuery query,
            CancellationToken cancellationToken
        )
        {
            var clients = db.Clients.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = $"%{query.Search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
                clients = clients.Where(c =>
                    EF.Functions.ILike(c.FirstName, pattern, "\\")
                    || EF.Functions.ILike(c.LastName, pattern, "\\")
                );
            }

            var total = await clients.CountAsync(cancellationToken);

            var items = await clients
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(c => new ClientResponse(
                    c.ID,
                    c.FirstName,
                    c.LastName,
                    c.BirthDate,
                    c.Phone,
                    c.Address
                ))
                .ToListAsync(cancellationToken);

            return Result.Ok(new PagedResult<ClientResponse>(items, total, query.Page, query.PageSize));
        }

        public async Task<Result<ClientResponse>> GetByIdAsync(
            int id,
            CancellationToken cancellationToken
        )
        {
            var client = await db
                .Clients.AsNoTracking()
                .Where(c => c.ID == id)
                .Select(c => new ClientResponse(
                    c.ID,
                    c.FirstName,
                    c.LastName,
                    c.BirthDate,
                    c.Phone,
                    c.Address
                ))
                .FirstOrDefaultAsync(cancellationToken);

            if (client is null)
                return Result.Fail<ClientResponse>(NotFound(id));

            return Result.Ok(client);
        }

        public async Task<Result<ClientResponse>> CreateAsync(
            CreateClientRequest request,
            string userId,
            CancellationToken cancellationToken
        )
        {
            var client = new Client
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                BirthDate = DateTime.SpecifyKind(request.BirthDate, DateTimeKind.Utc),
                Phone = request.Phone.Trim(),
                Address = request.Address.Trim(),
            };

            db.Clients.Add(client);
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                ApiEvents.ClientCreated,
                "Client {ClientId} created by {UserId}",
                client.ID,
                userId
            );

            return Result.Ok(
                new ClientResponse(
                    client.ID,
                    client.FirstName,
                    client.LastName,
                    client.BirthDate,
                    client.Phone,
                    client.Address
                )
            );
        }

        public async Task<Result> UpdateAsync(
            int id,
            UpdateClientRequest request,
            string userId,
            CancellationToken cancellationToken
        )
        {
            var client = await db.Clients.FirstOrDefaultAsync(c => c.ID == id, cancellationToken);

            if (client is null)
                return Result.Fail(NotFound(id));

            client.FirstName = request.FirstName.Trim();
            client.LastName = request.LastName.Trim();
            client.BirthDate = DateTime.SpecifyKind(request.BirthDate, DateTimeKind.Utc);
            client.Phone = request.Phone.Trim();
            client.Address = request.Address.Trim();

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                ApiEvents.ClientUpdated,
                "Client {ClientId} updated by {UserId}",
                id,
                userId
            );

            return Result.Ok();
        }

        public async Task<Result> DeleteAsync(
            int id,
            string userId,
            CancellationToken cancellationToken
        )
        {
            var client = await db.Clients.FirstOrDefaultAsync(c => c.ID == id, cancellationToken);

            if (client is null)
                return Result.Fail(NotFound(id));

            var hasOrders = await db.Orders.AnyAsync(o => o.ClientId == id, cancellationToken);

            if (hasOrders)
                return Result.Fail(
                    Error.Conflict(
                        "client.has_orders",
                        "No se puede borrar un cliente que tiene órdenes."
                    )
                );

            db.Clients.Remove(client);
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                ApiEvents.ClientDeleted,
                "Client {ClientId} deleted by {UserId}",
                id,
                userId
            );

            return Result.Ok();
        }

        private Error NotFound(int id)
        {
            logger.LogWarning(ApiEvents.ClientNotFound, "Client {ClientId} not found", id);
            return Error.NotFound("client.not_found", $"No existe el cliente {id}.");
        }
    }
}
