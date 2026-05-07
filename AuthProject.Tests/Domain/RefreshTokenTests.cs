using FluentAssertions;

namespace AuthProject.Tests.Domain;

public class RefreshTokenTests
{
    [Fact]
    public void Create_TokenNuevo_NoEstaExpirado()
    {
        // Arrange
        var token = RefreshToken.Create(Guid.NewGuid(), "mi-token", expirationDays: 7);

        // Assert
        token.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void Create_TokenExpirado_IsExpiredTrue()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "mi-token", expirationDays: -1);

        token.IsExpired.Should().BeTrue();
        token.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Revoke_TokenRevocado_NoEsValido()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "mi-token", expirationDays: 7);
        token.Revoke();

        token.IsRevoked.Should().BeTrue();
        token.IsValid.Should().BeFalse();
    }
}