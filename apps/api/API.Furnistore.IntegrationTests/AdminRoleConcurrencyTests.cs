using API.Furnistore.Application.Admin.Customers;
using API.Furnistore.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace API.Furnistore.IntegrationTests
{
    public sealed class AdminRoleConcurrencyTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
    {
        private readonly TestData data = new(fixture);

        [Fact]
        public async Task Simultaneous_revocations_always_leave_one_admin()
        {
            var admins = new List<(string UserId, int ClientId)>();
            for (var i = 0; i < 6; i++)
            {
                var account = await data.CreateClientAsync();
                var granted = await fixture.RunAsync<AdminCustomerService, Result<AdminCustomerResponse>>(service =>
                    service.GrantAdminAsync(account.ClientId, "console", CancellationToken.None)
                );
                Assert.True(granted.IsSuccess, granted.Error?.Message);
                admins.Add(account);
            }

            var results = await Task.WhenAll(
                admins.Select((admin, index) =>
                    Task.Run(() =>
                        fixture.RunAsync<AdminCustomerService, Result<AdminCustomerResponse>>(service =>
                            service.RevokeAdminAsync(
                                admins[(index + 1) % admins.Count].ClientId,
                                admin.UserId,
                                CancellationToken.None
                            )
                        )
                    )
                )
            );

            await using var db = fixture.CreateContext();
            Assert.Equal(1, await db.UserRoles.CountAsync());
            Assert.Equal(admins.Count - 1, results.Count(result => result.IsSuccess));
        }
    }
}
