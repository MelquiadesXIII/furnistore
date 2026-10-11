using API.Furnistore.Application.Admin.Products;
using API.Furnistore.Application.Clients;

namespace API.Furnistore.IntegrationTests
{
    public sealed class ProfileAndImageTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
    {
        private readonly TestData data = new(fixture);

        [Theory]
        [InlineData("05121467221", true)]
        [InlineData("85010312345", true)]
        [InlineData("00022968123", true)]
        [InlineData("05131467221", false)]
        [InlineData("05023067221", false)]
        [InlineData("0512146722", false)]
        [InlineData("0512146722a", false)]
        public void The_identity_card_needs_11_digits_starting_with_a_real_date(string value, bool valid) =>
            Assert.Equal(valid, IdentityCardAttribute.IsValidNumber(value));

        [Fact]
        public async Task The_profile_is_complete_only_with_an_identity_card_and_it_cannot_be_repeated()
        {
            var (userId, _) = await data.CreateClientAsync(profileComplete: false);
            var (otherUserId, _) = await data.CreateClientAsync(profileComplete: false);
            var identityCard = TestData.NewIdentityCard();

            var saved = await Clients(service => service.UpdateMeAsync(userId, Profile(identityCard), CancellationToken.None));
            Assert.True(saved.IsSuccess, saved.Error?.Message);

            var me = (await Clients(service => service.GetMeAsync(userId, CancellationToken.None))).Value;
            Assert.Equal(identityCard, me.IdentityCard);
            Assert.True(me.IsProfileComplete);

            var repeated = await Clients(service => service.UpdateMeAsync(otherUserId, Profile(identityCard), CancellationToken.None));
            Assert.Equal("client.identity_card_taken", repeated.Error!.Code);
        }

        [Fact]
        public async Task Uploading_a_product_image_checks_type_and_size()
        {
            using var photo = new MemoryStream(new byte[1024]);

            var uploaded = await Products(service => service.UploadImageAsync(photo, "image/jpeg", photo.Length, "admin-1", CancellationToken.None));
            Assert.True(uploaded.IsSuccess, uploaded.Error?.Message);
            Assert.EndsWith(".jpg", uploaded.Value.Url);
            Assert.Contains(fixture.Images.Uploaded, name => uploaded.Value.Url.EndsWith(name));

            var gif = await Products(service => service.UploadImageAsync(photo, "image/gif", photo.Length, "admin-1", CancellationToken.None));
            Assert.Equal("product.image_type", gif.Error!.Code);

            var huge = await Products(service =>
                service.UploadImageAsync(photo, "image/png", AdminProductService.MaxImageBytes + 1, "admin-1", CancellationToken.None)
            );
            Assert.Equal("product.image_size", huge.Error!.Code);

            fixture.Images.Fail = true;
            try
            {
                var failed = await Products(service => service.UploadImageAsync(photo, "image/webp", photo.Length, "admin-1", CancellationToken.None));
                Assert.Equal("product.image_upload_failed", failed.Error!.Code);
            }
            finally
            {
                fixture.Images.Fail = false;
            }
        }

        private static UpdateClientRequest Profile(string identityCard) =>
            new()
            {
                FirstName = "Ana",
                LastName = "Prueba",
                Phone = "+5355555555",
                IdentityCard = identityCard,
                Address = new ShippingAddress { Street = "Calle 10 #5", City = "Vedado", Province = "La Habana" },
            };

        private Task<T> Clients<T>(Func<ClientService, Task<T>> action) => fixture.RunAsync(action);

        private Task<T> Products<T>(Func<AdminProductService, Task<T>> action) => fixture.RunAsync(action);
    }
}
