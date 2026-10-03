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
        public async Task<Result<ClientResponse>> GetMeAsync(
            string userId,
            CancellationToken cancellationToken
        )
        {
            var row = await WithEmail()
                .Where(row => row.Client.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken);

            if (row is null)
                return Result.Fail<ClientResponse>(SelfNotFound());

            return Result.Ok(ToResponse(row.Client, row.Email));
        }

        public async Task<Result> UpdateMeAsync(
            string userId,
            UpdateClientRequest request,
            CancellationToken cancellationToken
        )
        {
            var client = await db.Clients.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

            if (client is null)
                return Result.Fail(SelfNotFound());

            Apply(client, request);

            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Fail(
                    Error.Conflict(
                        "client.version_conflict",
                        "Tus datos cambiaron mientras los editabas. Recarga e intenta de nuevo."
                    )
                );
            }

            logger.LogInformation(
                ApiEvents.ClientUpdated,
                "Client {ClientId} self-updated by {UserId}",
                client.ID,
                userId
            );

            return Result.Ok();
        }

        private static ShippingAddress? AddressOf(Client client) =>
            client.Street is null || client.City is null || client.Province is null
                ? null
                : new ShippingAddress
                {
                    Street = client.Street,
                    City = client.City,
                    Province = client.Province,
                    DeliveryNotes = client.DeliveryNotes,
                };

        private IQueryable<ClientRow> WithEmail() =>
            from client in db.Clients.AsNoTracking()
            join user in db.Users on client.UserId equals user.Id
            select new ClientRow { Client = client, Email = user.Email };

        private static void Apply(Client client, UpdateClientRequest request)
        {
            client.FirstName = request.FirstName.Trim();
            client.LastName = request.LastName.Trim();
            client.Phone = request.Phone.Trim();
            client.Street = request.Address.Street.Trim();
            client.City = request.Address.City.Trim();
            client.Province = request.Address.Province.Trim();
            client.DeliveryNotes = TextInput.NullIfBlank(request.Address.DeliveryNotes);
        }

        private static ClientResponse ToResponse(Client client, string? email) =>
            new(
                client.ID,
                email ?? string.Empty,
                client.FirstName,
                client.LastName,
                client.Phone,
                AddressOf(client),
                client.IsProfileComplete
            );

        private Error SelfNotFound()
        {
            logger.LogWarning(ApiEvents.ClientNotFound, "No client is linked to the authenticated user");
            return Error.NotFound(
                "client.self_not_found",
                "No se encontró un cliente asociado a esta cuenta."
            );
        }

        private sealed class ClientRow
        {
            public required Client Client { get; init; }

            public string? Email { get; init; }
        }
    }
}
