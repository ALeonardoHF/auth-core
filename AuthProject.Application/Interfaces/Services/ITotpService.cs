public interface ITotpService
{
    string GenerateSecret();
    string GenerateQrCodeBase64(string email, string secret);
    bool Verify(string secret, string code);
}
