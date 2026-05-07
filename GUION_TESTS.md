# Guión de Tests — AuthProject

## Setup del proyecto

```bash
# Ya creado — solo instalar dependencias si falta algo
dotnet add AuthProject.Tests package Moq
dotnet add AuthProject.Tests package FluentAssertions
dotnet add reference ../AuthProject.Application/AuthProject.Application.csproj
dotnet add reference ../AuthProject.Domain/AuthProject.Domain.csproj
```

---

## Estructura de carpetas

```
AuthProject.Tests/
├── Services/
│   ├── AuthServiceTests.cs
│   └── UserServiceTests.cs
└── Domain/
    └── RefreshTokenTests.cs
```

---

## Patrón base (AAA)

Cada test sigue siempre: **Arrange → Act → Assert**

```csharp
[Fact]
public async Task LoginAsync_WhenUserNotFound_ThrowsUnauthorizedException()
{
    // Arrange
    _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
                 .ReturnsAsync((User?)null);

    // Act
    var act = () => _authService.LoginAsync(new LoginRequest("x@x.com", "pass"), null, null);

    // Assert
    await act.Should().ThrowAsync<UnauthorizedException>();
}
```

---

## Mocks que necesitas en cada test class

### AuthServiceTests.cs
```csharp
private readonly Mock<IUserRepository> _userRepo = new();
private readonly Mock<IRefreshTokenRepository> _refreshTokenRepo = new();
private readonly Mock<IPasswordHasher> _hasher = new();
private readonly Mock<ITokenService> _tokenService = new();
private readonly AuthService _sut;

public AuthServiceTests()
{
    var settings = Options.Create(new JwtSettings { RefreshTokenExpirationDays = 7 });
    _sut = new AuthService(
        _userRepo.Object,
        _refreshTokenRepo.Object,
        _hasher.Object,
        _tokenService.Object,
        settings);
}
```

### UserServiceTests.cs
```csharp
private readonly Mock<IUserRepository> _userRepo = new();
private readonly Mock<IPasswordHasher> _hasher = new();
private readonly UserService _sut;

public UserServiceTests()
{
    _sut = new UserService(_userRepo.Object, _hasher.Object);
}
```

---

## 1. RefreshTokenTests.cs — Empieza aquí (sin mocks)

> Tests de entidad pura. No necesitas mocks, solo crear objetos.

| # | Escenario | Qué verificar |
|---|-----------|---------------|
| 1 | Token recién creado | `IsExpired == false` |
| 2 | Token recién creado | `IsRevoked == false` |
| 3 | Token recién creado | `IsValid == true` |
| 4 | Token creado con `expirationDays: -1` (ya expiró) | `IsExpired == true`, `IsValid == false` |
| 5 | Llamar `Revoke()` | `IsRevoked == true`, `IsValid == false` |
| 6 | `Create()` guarda UserId, Token, DeviceInfo, IpAddress | Propiedades correctas |

**Tip:** Para simular un token expirado usa `expirationDays: -1` en `RefreshToken.Create(...)`.

---

## 2. AuthServiceTests.cs

### LOGIN

| # | Escenario | Qué verificar |
|---|-----------|---------------|
| 1 | Credenciales correctas | Retorna `LoginResponse` con `AccessToken`, `RefreshToken`, `Role` |
| 2 | Email no existe (`GetByEmailAsync` retorna `null`) | Lanza `UnauthorizedException` |
| 3 | Password incorrecto (`Verify` retorna `false`) | Lanza `UnauthorizedException` |
| 4 | Usuario con `IsActive = false` | Lanza `UnauthorizedException` |
| 5 | Login exitoso | `AddAsync` en `refreshTokenRepo` se llama 1 vez |

**Tip para #4:** Usa `user.Deactivate()` después de crear el usuario con `User.Create(...)`.

### REFRESH

| # | Escenario | Qué verificar |
|---|-----------|---------------|
| 6 | Token válido | Retorna nuevos tokens |
| 7 | Token válido | El token anterior queda con `IsRevoked == true` |
| 8 | Token válido | Se llama `UpdateAsync` con el token viejo y `AddAsync` con uno nuevo |
| 9 | Token inexistente (`GetByTokenAsync` retorna `null`) | Lanza `UnauthorizedException` |
| 10 | Token ya revocado | Lanza `UnauthorizedException` |
| 11 | Token expirado (`expirationDays: -1`) | Lanza `UnauthorizedException` |
| 12 | Usuario inactivo | Lanza `UnauthorizedException` |

**Tip para #10 y #11:** Crea el `RefreshToken` con `RefreshToken.Create(...)` y luego llama `.Revoke()` o usa `expirationDays: -1`.

### LOGOUT

| # | Escenario | Qué verificar |
|---|-----------|---------------|
| 13 | Token válido | `IsRevoked == true` y `UpdateAsync` se llama 1 vez |
| 14 | Token inexistente | No lanza excepción (silencioso) |
| 15 | Token ya revocado | No lanza excepción (silencioso) |

### LOGOUT ALL DEVICES

| # | Escenario | Qué verificar |
|---|-----------|---------------|
| 16 | Llamar `LogoutAllDevicesAsync` | `RevokeAllByUserIdAsync` se llama 1 vez con el userId correcto |
| 17 | Llamar `LogoutAllDevicesAsync` | `user.TokenVersion` se incrementa en 1 |

---

## 3. UserServiceTests.cs

| # | Escenario | Qué verificar |
|---|-----------|---------------|
| 1 | Email nuevo (`ExistsWithEmailAsync` retorna `false`) | Retorna `UserResponse` con datos correctos |
| 2 | Email duplicado (`ExistsWithEmailAsync` retorna `true`) | Lanza `DomainException` |
| 3 | `GetByIdAsync` con Id existente | Retorna `UserResponse` |
| 4 | `GetByIdAsync` con Id inexistente (`GetByIdAsync` retorna `null`) | Lanza `NotFoundException` |
| 5 | Crear usuario | `AddAsync` se llama 1 vez |
| 6 | Crear usuario | `Hash` del hasher se llama 1 vez |

---

## Orden recomendado de implementación

```
1. RefreshTokenTests.cs     ← dominio puro, sin mocks, más fácil
2. UserServiceTests.cs      ← 2 mocks sencillos
3. AuthServiceTests.cs      ← más mocks, más escenarios
```

## Correr los tests

```bash
dotnet test
dotnet test --verbosity normal        # ver nombre de cada test
dotnet test --filter "Login"          # filtrar por nombre
```
