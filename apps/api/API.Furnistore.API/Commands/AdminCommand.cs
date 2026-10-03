using API.Furnistore.Application.Admin.Audit;
using API.Furnistore.Application.Admin.Customers;

namespace API.Furnistore.API.Commands
{
    public static class AdminCommand
    {
        private const string Usage = "Uso: dotnet run --project API.Furnistore.API -- admin promote <correo>";

        public static async Task<bool> TryRunAsync(WebApplication app, string[] args)
        {
            if (args.Length == 0 || args[0] != "admin")
                return false;

            if (args.Length != 3 || args[1] != "promote")
            {
                Console.Error.WriteLine(Usage);
                Environment.ExitCode = 2;
                return true;
            }

            await using var scope = app.Services.CreateAsyncScope();
            var customers = scope.ServiceProvider.GetRequiredService<AdminCustomerService>();
            var email = args[2];

            var id = await customers.FindIdByEmailAsync(email, CancellationToken.None);
            if (id is null)
            {
                Console.Error.WriteLine($"No existe una cuenta con el correo {email}.");
                Environment.ExitCode = 1;
                return true;
            }

            var result = await customers.GrantAdminAsync(id.Value, AuditActors.Console, CancellationToken.None);
            if (!result.IsSuccess)
            {
                Console.Error.WriteLine(result.Error!.Message);
                Environment.ExitCode = 1;
                return true;
            }

            Console.WriteLine($"{email} ahora tiene el rol de administrador. Tiene que cerrar sesión y volver a entrar para ver el panel.");
            return true;
        }
    }
}
