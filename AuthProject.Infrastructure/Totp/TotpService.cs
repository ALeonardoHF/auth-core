using OtpNet;
using QRCoder;

public class TotpService : ITotpService
{
    public string GenerateSecret()
    {
        var secret = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(secret);
    }

    public string GenerateQrCodeBase64(string email, string secret)
    {
        var uri = $"otpauth://totp/AuthCore:{email}?secret={secret}&issuer=AuthCore";
        var qrGenerator = new QRCodeGenerator();
        var qrCodeData  = qrGenerator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
        var qrCode      = new PngByteQRCode(qrCodeData);
        return Convert.ToBase64String(qrCode.GetGraphic(20));
    }

    public bool Verify(string secret, string code)
    {
        var totp = new Totp(Base32Encoding.ToBytes(secret));
        return totp.VerifyTotp(DateTime.UtcNow, code, out _, new VerificationWindow(2, 2));
    }
}
