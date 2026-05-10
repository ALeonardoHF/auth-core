public static class EmailTemplates
{
    public static string ConfirmationSuccess() => """
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8"></head>
        <body style="margin:0;padding:0;background-color:#1A0A2E;font-family:Arial,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0">
            <tr>
              <td align="center" style="padding:40px 20px;">
                <table width="520" cellpadding="0" cellspacing="0" style="background-color:#0D0D0D;border-radius:16px;overflow:hidden;border:1px solid #6B3FA0;">

                  <tr>
                    <td style="background:linear-gradient(135deg,#1A0A2E,#6B3FA0);padding:40px;text-align:center;">
                      <div style="font-size:48px;">👻</div>
                      <h1 style="color:#C9A8E0;font-size:26px;margin:12px 0 0;">AuthCore</h1>
                    </td>
                  </tr>

                  <tr>
                    <td style="padding:40px;text-align:center;">
                      <div style="font-size:56px;margin-bottom:16px;">✅</div>
                      <h2 style="color:#C9A8E0;font-size:22px;margin:0 0 12px;">¡Cuenta confirmada!</h2>
                      <p style="color:#F2C4B8;font-size:15px;line-height:1.6;margin:0;">
                        Tu cuenta está activa. Ya puedes iniciar sesión.
                      </p>
                    </td>
                  </tr>

                  <tr>
                    <td style="padding:20px 40px;border-top:1px solid #2A1A4E;">
                      <p style="color:#9B6DC5;font-size:12px;margin:0;text-align:center;">
                        AuthCore — Sistema de autenticación seguro
                      </p>
                    </td>
                  </tr>

                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;

    public static string Confirmation(string confirmationLink) => $"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8"></head>
        <body style="margin:0;padding:0;background-color:#1A0A2E;font-family:Arial,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0">
            <tr>
              <td align="center" style="padding:40px 20px;">
                <table width="520" cellpadding="0" cellspacing="0" style="background-color:#0D0D0D;border-radius:16px;overflow:hidden;border:1px solid #6B3FA0;">

                  <tr>
                    <td style="background:linear-gradient(135deg,#1A0A2E,#6B3FA0);padding:40px;text-align:center;">
                      <div style="font-size:48px;">👻</div>
                      <h1 style="color:#C9A8E0;font-size:26px;margin:12px 0 0;">AuthCore</h1>
                    </td>
                  </tr>

                  <tr>
                    <td style="padding:40px;">
                      <h2 style="color:#C9A8E0;font-size:20px;margin:0 0 16px;">Confirma tu cuenta</h2>
                      <p style="color:#F2C4B8;font-size:15px;line-height:1.6;margin:0 0 32px;">
                        Haz clic en el botón de abajo para activar tu cuenta. El link expira en <strong style="color:#C9A8E0;">24 horas</strong>.
                      </p>
                      <div style="text-align:center;">
                        <a href="{confirmationLink}"
                           style="display:inline-block;background-color:#6B3FA0;color:#ffffff;text-decoration:none;
                                  padding:14px 36px;border-radius:8px;font-size:15px;font-weight:bold;
                                  border:1px solid #9B6DC5;">
                          Confirmar cuenta
                        </a>
                      </div>
                    </td>
                  </tr>

                  <tr>
                    <td style="padding:20px 40px;border-top:1px solid #2A1A4E;">
                      <p style="color:#9B6DC5;font-size:12px;margin:0;text-align:center;">
                        Si no creaste esta cuenta puedes ignorar este mensaje.
                      </p>
                    </td>
                  </tr>

                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;

    public static string PasswordReset(string resetLink) => $"""
    <!DOCTYPE html>
    <html>
    <head><meta charset="utf-8"></head>
    <body style="margin:0;padding:0;background-color:#1A0A2E;font-family:Arial,sans-serif;">
      <table width="100%" cellpadding="0" cellspacing="0">
        <tr>
          <td align="center" style="padding:40px 20px;">
            <table width="520" cellpadding="0" cellspacing="0" style="background-color:#0D0D0D;border-radius:16px;overflow:hidden;border:1px solid #6B3FA0;">

              <tr>
                <td style="background:linear-gradient(135deg,#1A0A2E,#6B3FA0);padding:40px;text-align:center;">
                  <div style="font-size:48px;">👻</div>
                  <h1 style="color:#C9A8E0;font-size:26px;margin:12px 0 0;">AuthCore</h1>
                </td>
              </tr>

              <tr>
                <td style="padding:40px;">
                  <h2 style="color:#C9A8E0;font-size:20px;margin:0 0 16px;">Restablecer contraseña</h2>
                  <p style="color:#F2C4B8;font-size:15px;line-height:1.6;margin:0 0 32px;">
                    Recibimos una solicitud para restablecer tu contraseña. El link expira en <strong style="color:#C9A8E0;">1 hora</strong>.
                  </p>
                  <div style="text-align:center;">
                    <a href="{resetLink}"
                       style="display:inline-block;background-color:#6B3FA0;color:#ffffff;text-decoration:none;
                              padding:14px 36px;border-radius:8px;font-size:15px;font-weight:bold;
                              border:1px solid #9B6DC5;">
                      Restablecer contraseña
                    </a>
                  </div>
                </td>
              </tr>

              <tr>
                <td style="padding:20px 40px;border-top:1px solid #2A1A4E;">
                  <p style="color:#9B6DC5;font-size:12px;margin:0;text-align:center;">
                    Si no solicitaste esto puedes ignorar este mensaje.
                  </p>
                </td>
              </tr>

            </table>
          </td>
        </tr>
      </table>
    </body>
    </html>
    """;

    public static string ResetPasswordForm(string token) => $"""
    <!DOCTYPE html>
    <html>
    <head><meta charset="utf-8"><title>Restablecer contraseña</title></head>
    <body style="margin:0;padding:0;background-color:#1A0A2E;font-family:Arial,sans-serif;">
      <table width="100%" cellpadding="0" cellspacing="0">
        <tr>
          <td align="center" style="padding:40px 20px;">
            <table width="520" cellpadding="0" cellspacing="0" style="background-color:#0D0D0D;border-radius:16px;overflow:hidden;border:1px solid #6B3FA0;">

              <tr>
                <td style="background:linear-gradient(135deg,#1A0A2E,#6B3FA0);padding:40px;text-align:center;">
                  <div style="font-size:48px;">👻</div>
                  <h1 style="color:#C9A8E0;font-size:26px;margin:12px 0 0;">AuthCore</h1>
                </td>
              </tr>

              <tr>
                <td style="padding:40px;">
                  <h2 style="color:#C9A8E0;font-size:20px;margin:0 0 24px;">Nueva contraseña</h2>
                  <form method="POST" action="/auth/reset-password-form">
                    <input type="hidden" name="token" value="{token}" />
                    <div style="margin-bottom:16px;">
                      <input type="password" name="newPassword" placeholder="Nueva contraseña"
                        style="width:100%;padding:12px;border-radius:8px;border:1px solid #6B3FA0;
                               background:#1A0A2E;color:#F2C4B8;font-size:15px;box-sizing:border-box;" />
                    </div>
                    <div style="text-align:center;">
                      <button type="submit"
                        style="background-color:#6B3FA0;color:#fff;border:none;padding:14px 36px;
                               border-radius:8px;font-size:15px;font-weight:bold;cursor:pointer;
                               border:1px solid #9B6DC5;">
                        Restablecer contraseña
                      </button>
                    </div>
                  </form>
                </td>
              </tr>

              <tr>
                <td style="padding:20px 40px;border-top:1px solid #2A1A4E;">
                  <p style="color:#9B6DC5;font-size:12px;margin:0;text-align:center;">
                    AuthCore — Sistema de autenticación seguro
                  </p>
                </td>
              </tr>

            </table>
          </td>
        </tr>
      </table>
    </body>
    </html>
    """;

    public static string PasswordResetSuccess() => """
    <!DOCTYPE html>
    <html>
    <head><meta charset="utf-8"></head>
    <body style="margin:0;padding:0;background-color:#1A0A2E;font-family:Arial,sans-serif;">
      <table width="100%" cellpadding="0" cellspacing="0">
        <tr>
          <td align="center" style="padding:40px 20px;">
            <table width="520" cellpadding="0" cellspacing="0" style="background-color:#0D0D0D;border-radius:16px;overflow:hidden;border:1px solid #6B3FA0;">

              <tr>
                <td style="background:linear-gradient(135deg,#1A0A2E,#6B3FA0);padding:40px;text-align:center;">
                  <div style="font-size:48px;">👻</div>
                  <h1 style="color:#C9A8E0;font-size:26px;margin:12px 0 0;">AuthCore</h1>
                </td>
              </tr>

              <tr>
                <td style="padding:40px;text-align:center;">
                  <div style="font-size:56px;margin-bottom:16px;">🔐</div>
                  <h2 style="color:#C9A8E0;font-size:22px;margin:0 0 12px;">¡Contraseña actualizada!</h2>
                  <p style="color:#F2C4B8;font-size:15px;line-height:1.6;margin:0;">
                    Tu contraseña fue restablecida correctamente. Ya puedes iniciar sesión.
                  </p>
                </td>
              </tr>

              <tr>
                <td style="padding:20px 40px;border-top:1px solid #2A1A4E;">
                  <p style="color:#9B6DC5;font-size:12px;margin:0;text-align:center;">
                    AuthCore — Sistema de autenticación seguro
                  </p>
                </td>
              </tr>

            </table>
          </td>
        </tr>
      </table>
    </body>
    </html>
    """;

}
