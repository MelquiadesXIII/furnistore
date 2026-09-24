using API.Furnistore.API.Services;
using API.Furnistore.Application.Auth;
using API.Furnistore.Application.Carts;
using API.Furnistore.Application.Clients;
using API.Furnistore.Application.Orders;
using API.Furnistore.Application.ProductCategories;
using API.Furnistore.Application.Products;

namespace API.Furnistore.API.Extensions
{
    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<ProductService>();
            services.AddScoped<ProductCategoryService>();
            services.AddScoped<ClientService>();
            services.AddScoped<OrderService>();
            services.AddScoped<CartService>();
            services.AddScoped<AuthService>();

            services.AddSingleton<IVerificationEmailSender, SmtpEmailSender>();
            services.AddSingleton(TimeProvider.System);

            return services;
        }
    }
}
