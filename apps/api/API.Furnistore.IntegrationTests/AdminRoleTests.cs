using API.Furnistore.Application.Admin.Customers;
using API.Furnistore.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace API.Furnistore.IntegrationTests
{
    public sealed class AdminRoleTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
    {
        private readonly TestData data = new(fixture);

        [Fact]
        public async Task Admin_roles_protect_the_actor_and_the_last_admin()
        {
            var (firstUser, first) = await data.CreateClientAsync();
            var (secondUser, second) = await data.CreateClientAsync();

            var promoted = await Act(service => service.GrantAdminAsync(first, "console", CancellationToken.None));
            Assert.True(promoted.IsSuccess, promoted.Error?.Message);
            Assert.True(promoted.Value.Account.IsAdmin);
            Assert.Equal("customer.admin_granted", Assert.Single(promoted.Value.History).Action);

            var twice = await Act(service => service.GrantAdminAsync(first, "console", CancellationToken.None));
            Assert.Equal("customer.already_admin", twice.Error!.Code);

            var self = await Act(service => service.RevokeAdminAsync(first, firstUser, CancellationToken.None));
            Assert.Equal("customer.self_action", self.Error!.Code);

            var last = await Act(service => service.RevokeAdminAsync(first, secondUser, CancellationToken.None));
            Assert.Equal("customer.last_admin", last.Error!.Code);

            var disableAdmin = await Act(service => service.DisableAsync(first, secondUser, CancellationToken.None));
            Assert.Equal("customer.is_admin", disableAdmin.Error!.Code);

            await Act(service => service.GrantAdminAsync(second, firstUser, CancellationToken.None));

            var mutual = await Task.WhenAll(
                Act(service => service.RevokeAdminAsync(first, secondUser, CancellationToken.None)),
                Act(service => service.RevokeAdminAsync(second, firstUser, CancellationToken.None))
            );
            Assert.Single(mutual, result => result.IsSuccess);
            Assert.Single(mutual, result => result.Error?.Code == "customer.last_admin");

            await using var db = fixture.CreateContext();
            Assert.Equal(1, await db.UserRoles.CountAsync());

            var admins = await fixture.RunAsync<AdminCustomerService, Result<PagedResult<AdminCustomerSummary>>>(service =>
                service.SearchAsync(new AdminCustomerQuery { Filter = CustomerFilter.Admins }, CancellationToken.None)
            );
            Assert.Single(admins.Value.Items);
        }

        [Fact]
        public async Task Nobody_can_disable_their_own_account_and_unconfirmed_accounts_cannot_be_admins()
        {
            var (userId, clientId) = await data.CreateClientAsync();

            var self = await Act(service => service.DisableAsync(clientId, userId, CancellationToken.None));
            Assert.Equal("customer.self_action", self.Error!.Code);

            await using (var db = fixture.CreateContext())
            {
                var user = await db.Users.SingleAsync(u => u.Id == userId);
                user.EmailConfirmed = false;
                await db.SaveChangesAsync();
            }

            var unconfirmed = await Act(service => service.GrantAdminAsync(clientId, "console", CancellationToken.None));
            Assert.Equal("customer.unconfirmed", unconfirmed.Error!.Code);
        }

        private Task<Result<AdminCustomerResponse>> Act(Func<AdminCustomerService, Task<Result<AdminCustomerResponse>>> action) =>
            fixture.RunAsync(action);
    }
}
