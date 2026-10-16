using System.Text.RegularExpressions;
using API.Furnistore.Application.Auth;

namespace API.Furnistore.API.Services;

/// <summary>
/// Implementación de IVerificationEmailSender para desarrollo.
/// En lugar de enviar el correo, imprime el contenido en el log
/// para que puedas leer el código de verificación en la terminal.
/// NO USAR EN PRODUCCIÓN.
/// </summary>
public sealed partial class LogVerificationEmailSender(ILogger<LogVerificationEmailSender> logger)
    : IVerificationEmailSender
{
    [GeneratedRegex(@"\b\d{6}\b")]
    private static partial Regex CodePattern();

    public Task SendAsync(EmailMessage email, CancellationToken cancellationToken)
    {
        var match = CodePattern().Match(email.Subject);
        var code = match.Success ? match.Value : "(no encontrado)";

        logger.LogWarning(
            "\n" +
            "╔══════════════════════════════════════════════════════╗\n" +
            "║         CORREO DE VERIFICACIÓN (modo dev)            ║\n" +
            "╠══════════════════════════════════════════════════════╣\n" +
            "║ Para:   {To}\n" +
            "║ Asunto: {Subject}\n" +
            "║                                                      ║\n" +
            "║   CÓDIGO →  {Code}                                  ║\n" +
            "║                                                      ║\n" +
            "╚══════════════════════════════════════════════════════╝",
            email.To,
            email.Subject,
            code
        );

        return Task.CompletedTask;
    }
}