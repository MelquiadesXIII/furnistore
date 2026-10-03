using API.Furnistore.API.Services;
using API.Furnistore.Application.Admin.Audit;
using API.Furnistore.Application.Admin.Categories;
using API.Furnistore.Application.Admin.Customers;
using API.Furnistore.Application.Admin.Orders;
using API.Furnistore.Application.Admin.Products;
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
            services.AddScoped<OrderWorkflow>();
            services.AddScoped<AuditLog>();
            services.AddScoped<AuditService>();
            services.AddScoped<AdminOrderService>();
            services.AddScoped<AdminProductService>();
            services.AddScoped<AdminCategoryService>();
            services.AddScoped<AdminCustomerService>();

            services.AddSingleton<IVerificationEmailSender, SmtpEmailSender>();
            services.AddSingleton(TimeProvider.System);

            return services;
        }
    }
}
