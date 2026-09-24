using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace API.Furnistore.Application.Auth
{
    internal static class AuthEmails
    {
        private static readonly HtmlEncoder Html = HtmlEncoder.Create(UnicodeRanges.All);

        public static EmailMessage VerificationCode(string to, string firstName, string code, TimeSpan lifetime)
        {
            var name = Html.Encode(firstName);
            var minutes = (int)lifetime.TotalMinutes;

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
                                <p style="margin:0 0 24px;font-size:16px;line-height:1.5;">Usa este código para confirmar tu correo y empezar a comprar:</p>
                                <p style="margin:0 0 24px;font-size:36px;font-weight:bold;letter-spacing:8px;font-family:'Courier New',monospace;color:#8f6526;">{code}</p>
                                <p style="margin:0 0 8px;font-size:13px;color:#6b6255;">El código vence en {minutes} minutos.</p>
                                <p style="margin:0;font-size:13px;color:#6b6255;">Si no creaste esta cuenta, ignora este mensaje. Nunca compartas este código.</p>
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

                Tu código para confirmar tu correo en Furnistore es: {code}

                Vence en {minutes} minutos. Si no creaste esta cuenta, ignora este mensaje. Nunca compartas este código.
                """;

            return new EmailMessage(to, $"Tu código de Furnistore: {code}", html, text);
        }
    }
}
