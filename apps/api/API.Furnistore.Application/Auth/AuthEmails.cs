using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace API.Furnistore.Application.Auth
{
    internal static class AuthEmails
    {
        private static readonly HtmlEncoder Html = HtmlEncoder.Create(UnicodeRanges.All);

        public static EmailMessage EmailConfirmation(string to, string firstName, string link)
        {
            var name = Html.Encode(firstName);
            var href = Html.Encode(link);

            var html = $"""
                <!doctype html>
                <html lang="es">
                  <body style="margin:0;padding:0;background:#ede7dd;font-family:Arial,Helvetica,sans-serif;color:#211c16;">
                    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="padding:32px 16px;">
                      <tr>
                        <td align="center">
                          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:480px;background:#faf7f2;border:1px solid #ddd5c6;border-radius:4px;padding:32px;">
                            <tr>
                              <td>
                                <p style="margin:0 0 24px;font-size:20px;font-weight:bold;">Furnistore</p>
                                <p style="margin:0 0 16px;font-size:16px;">Hola {name}:</p>
                                <p style="margin:0 0 24px;font-size:16px;line-height:1.5;">Gracias por crear tu cuenta. Confirma tu correo para empezar a comprar.</p>
                                <p style="margin:0 0 24px;">
                                  <a href="{href}" style="display:inline-block;background:#8f6526;color:#ffffff;text-decoration:none;font-size:16px;padding:12px 24px;border-radius:4px;">Confirmar mi correo</a>
                                </p>
                                <p style="margin:0 0 8px;font-size:13px;color:#6b6255;">Si el botón no funciona, copia este enlace en tu navegador:</p>
                                <p style="margin:0 0 24px;font-size:13px;word-break:break-all;"><a href="{href}" style="color:#8f6526;">{href}</a></p>
                                <p style="margin:0;font-size:13px;color:#6b6255;">Si no creaste esta cuenta, ignora este mensaje.</p>
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>
                    </table>
                  </body>
                </html>
                """;

            var text = $"""
                Hola {firstName}:

                Gracias por crear tu cuenta en Furnistore. Confirma tu correo abriendo este enlace:

                {link}

                Si no creaste esta cuenta, ignora este mensaje.
                """;

            return new EmailMessage(to, "Confirma tu correo en Furnistore", html, text);
        }
    }
}
