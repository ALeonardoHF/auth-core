# AuthProject — Guion de Construcción Paso a Paso

> Objetivo: Sistema de autenticación robusto, reutilizable, nivel producción.
> Metodología: Cada fase tiene decisiones, implementación y criterios de evaluación.
> Antes de avanzar a la siguiente fase: marca cada checklist.

---

## FASE 0 — Decisiones de Diseño (NO escribir código aún)

> Esta fase existe porque las decisiones de aquí cambian TODO lo que viene después.
> Responde cada pregunta y documenta tu elección con el motivo.

### 0.1 Tipo de PK

| Opción | Ventaja | Desventaja |
|--------|---------|------------|
| `int` (autoincrement) | Simple, rápido en queries, legible en logs | Enumerable desde API, problemático en sistemas distribuidos |
| `Guid` | No enumerable, seguro en URLs, listo para microservicios | Ligeramente más lento en índices |

**Decisión recomendada:** `Guid` para `User` y `RefreshToken`. `int` para tablas de catálogo (roles, permisos).

- [X ] Decidido: __Guid________ Motivo: __Mayor seguridad________

---

### 0.2 ¿Permisos por Rol o por Usuario?

| Opción | Cuándo usarla |
|--------|---------------|
| Permisos por **Rol** | Todos los admins tienen los mismos permisos. Más simple. |
| Permisos por **Usuario** | Cada usuario puede tener permisos personalizados. Más flexible. |
| **Híbrido** | Usuario hereda permisos del rol, pero puede tener overrides. Más complejo. |

**Decisión recomendada:** Permisos por Rol (JSON). Un override de usuario se puede agregar en v2.

- [X ] Decidido: __Rol________ Motivo: _Centralizado el cambio y si se necesita hacer un update es mas facil._________

---

### 0.3 ¿Qué va dentro del JWT?

| Opción | Ventaja | Desventaja |
|--------|---------|------------|
| Solo `sub` + `role` | Token pequeño, simple | Para verificar permisos hay que ir a DB |
| `sub` + `role` + `permissions` | Sin roundtrip a DB | Token pesado, permisos pueden quedar stale |

**Decisión recomendada:** `sub` + `email` + `role` en JWT. Permisos se verifican en middleware contra DB (con cache si crece).

- [ X] Decidido: ___sub y role_______ Motivo: ___por el momento, se que puede ser escalable en caso de que se use el proyectos grandes_______

---

### 0.4 Multi-dispositivo

| Opción | Comportamiento |
|--------|---------------|
| Un refresh token por usuario | Login en otro dispositivo revoca el anterior |
| Múltiples refresh tokens | Cada dispositivo tiene su sesión independiente |

**Decisión recomendada:** Múltiples refresh tokens. Guardas `DeviceInfo` (User-Agent) e IP por token.

- [ X] Decidido: __Multiples refresh tokens________ Motivo: __flexibilidad al usuario de usar mas de 1 dispositivo, centralizar el acceso a los dispositivos con un device manager________

---

### 0.5 Rate Limiting

| Opción | Dónde vive |
|--------|-----------|
| Middleware ASP.NET (`AspNetCoreRateLimit`) | En el código, sin depender de infra |
| Reverse proxy (Nginx, Cloudflare) | Fuera del código, más performante |

**Decisión recomendada:** Middleware en código para que sea autocontenido y portátil.

- [X ] Decidido: ____Middleware de ASP.NET______ Motivo: ___no depende de nadie mas, solo de codigo_______

---

### 0.6 Almacenamiento del JWT Secret

| Ambiente | Dónde guardar |
|----------|--------------|
| Desarrollo | `appsettings.Development.json` (nunca en git) |
| Producción | Variable de entorno o Secret Manager |

- [ X] Confirmado: `.gitignore` incluirá `appsettings.Development.json`

---

### Checklist Fase 0

- [ X] Todas las decisiones tomadas y documentadas
- [X ] No hay ningún "lo decido después"

---

## FASE 1 — Estructura del Proyecto

### 1.1 Crear la solución

```bash
dotnet new sln -n AuthProject
dotnet new webapi -n AuthProject.Api --no-openapi
dotnet new classlib -n AuthProject.Application
dotnet new classlib -n AuthProject.Domain
dotnet new classlib -n AuthProject.Infrastructure
dotnet new classlib -n AuthProject.Persistence

dotnet sln add AuthProject.Api
dotnet sln add AuthProject.Application
dotnet sln add AuthProject.Domain
dotnet sln add AuthProject.Infrastructure
dotnet sln add AuthProject.Persistence
```

### 1.2 Referencias entre proyectos

```
Api            → Application, Infrastructure
Application    → Domain
Infrastructure → Application, Domain, Persistence
Persistence    → Domain
```

```bash
dotnet add AuthProject.Api reference AuthProject.Application AuthProject.Infrastructure
dotnet add AuthProject.Application reference AuthProject.Domain
dotnet add AuthProject.Infrastructure reference AuthProject.Application AuthProject.Domain AuthProject.Persistence
dotnet add AuthProject.Persistence reference AuthProject.Domain
```

### 1.3 Paquetes NuGet por proyecto

**AuthProject.Api**
```bash
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Swashbuckle.AspNetCore
dotnet add package AspNetCoreRateLimit
```

**AuthProject.Infrastructure**
```bash
dotnet add package BCrypt.Net-Next
dotnet add package Microsoft.IdentityModel.Tokens
dotnet add package System.IdentityModel.Tokens.Jwt
```

**AuthProject.Persistence**
```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer   # o Sqlite para dev
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.EntityFrameworkCore.Design
```

### 1.4 Estructura de carpetas interna

```
AuthProject.Domain/
  Entities/
  Enums/
  Exceptions/

AuthProject.Application/
  DTOs/
    Auth/
    Users/
  Interfaces/
    Repositories/
    Services/
  Services/

AuthProject.Infrastructure/
  Security/       ← JWT, hashing
  Providers/      ← fecha, GUID (para testing)

AuthProject.Persistence/
  Context/
  Repositories/
  Migrations/
  Configurations/  ← IEntityTypeConfiguration<T>
  Seeds/

AuthProject.Api/
  Controllers/
  Middleware/
  Extensions/     ← DI registration
```

### Checklist Fase 1

- [ X] Solución compila sin errores
- [ X] Referencias entre proyectos correctas (sin referencias circulares)
- [ X] Carpetas creadas aunque estén vacías
- [X ] `.gitignore` configurado (excluye `bin/`, `obj/`, `*.user`, `appsettings.*.json` con secretos)

---

## FASE 2 — Domain Layer

> Regla de oro: El dominio NO depende de nada externo. Sin EF, sin ASP.NET, sin NuGet ajenos.

### 2.1 Enums

**`Enums/Role.cs`**
```csharp
public enum Role
{
    Client = 1,
    Helper = 2,
    Admin = 3
}
```

> Por qué empezar en 1: evita que `default(Role)` sea un rol válido.

### 2.2 Entidad User

**`Entities/User.cs`**
```csharp
public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public Role Role { get; private set; }
    public bool IsActive { get; private set; }
    public int TokenVersion { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? LastLogin { get; private set; }
    public bool IsDeleted { get; private set; }

    private readonly List<RefreshToken> _refreshTokens = new();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    // Constructor privado para EF
    private User() { }

    // Factory method — única forma de crear un usuario válido
    public static User Create(string email, string passwordHash, Role role)
    {
        // Validaciones de dominio aquí
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email is required.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password hash is required.");

        return new User
        {
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant().Trim(),
            PasswordHash = passwordHash,
            Role = role,
            IsActive = true,
            TokenVersion = 1,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public void RecordLogin() => LastLogin = DateTime.UtcNow;

    public void IncrementTokenVersion()
    {
        TokenVersion++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

### 2.3 Entidad RefreshToken

**`Entities/RefreshToken.cs`**
```csharp
public class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Token { get; private set; }
    public string? DeviceInfo { get; private set; }
    public string? IpAddress { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private RefreshToken() { }

    public static RefreshToken Create(
        Guid userId,
        string token,
        int expirationDays,
        string? deviceInfo = null,
        string? ipAddress = null)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            DeviceInfo = deviceInfo,
            IpAddress = ipAddress,
            ExpiresAt = DateTime.UtcNow.AddDays(expirationDays),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsValid => !IsRevoked && !IsExpired;

    public void Revoke() => IsRevoked = true;
}
```

### 2.4 Entidad RolePermission

**`Entities/RolePermission.cs`**
```csharp
public class RolePermission
{
    public int Id { get; private set; }
    public Role Role { get; private set; }
    public string PermissionsJson { get; private set; }

    private RolePermission() { }

    public static RolePermission Create(Role role, string permissionsJson)
    {
        return new RolePermission
        {
            Role = role,
            PermissionsJson = permissionsJson
        };
    }
}
```

### 2.5 Excepciones de Dominio

**`Exceptions/DomainException.cs`**
```csharp
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
```

### Checklist Fase 2

- [ ] Todas las entidades tienen constructor privado para EF
- [ ] Setters privados en todas las propiedades
- [ ] Toda creación pasa por factory method
- [ ] Validaciones de dominio en los factory methods
- [ ] `IsExpired` e `IsValid` son computed, no campos
- [ ] El proyecto Domain compila sin referencias externas ajenas
- [ ] No hay referencia a EF, ASP.NET ni ningún NuGet en Domain

---

## FASE 3 — Application Layer (Interfaces + DTOs)

> Regla: Application define contratos. Infrastructure los implementa.

### 3.1 Interfaces de Repositorios

**`Interfaces/Repositories/IUserRepository.cs`**
```csharp
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<bool> ExistsWithEmailAsync(string email);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task<IEnumerable<User>> GetAllAsync();
}
```

**`Interfaces/Repositories/IRefreshTokenRepository.cs`**
```csharp
public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task AddAsync(RefreshToken token);
    Task UpdateAsync(RefreshToken token);
    Task RevokeAllByUserIdAsync(Guid userId);
    Task<IEnumerable<RefreshToken>> GetActiveByUserIdAsync(Guid userId);
}
```

### 3.2 Interfaces de Servicios de Infraestructura

**`Interfaces/Services/IPasswordHasher.cs`**
```csharp
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
```

**`Interfaces/Services/ITokenService.cs`**
```csharp
public interface ITokenService
{
    string GenerateJwt(User user);
    string GenerateRefreshToken();
}
```

### 3.3 DTOs

**`DTOs/Auth/LoginRequest.cs`**
```csharp
public record LoginRequest(string Email, string Password);
```

**`DTOs/Auth/LoginResponse.cs`**
```csharp
public record LoginResponse(string AccessToken, string RefreshToken, string Role);
```

**`DTOs/Auth/RefreshTokenRequest.cs`**
```csharp
public record RefreshTokenRequest(string RefreshToken);
```

**`DTOs/Users/CreateUserRequest.cs`**
```csharp
public record CreateUserRequest(string Email, string Password, Role Role);
```

**`DTOs/Users/UserResponse.cs`**
```csharp
public record UserResponse(Guid Id, string Email, string Role, bool IsActive, DateTime CreatedAt);
```

### 3.4 Interfaces de Application Services

**`Interfaces/Services/IAuthService.cs`**
```csharp
public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, string? deviceInfo, string? ipAddress);
    Task<LoginResponse> RefreshAsync(string refreshToken, string? deviceInfo, string? ipAddress);
    Task LogoutAsync(string refreshToken);
    Task LogoutAllDevicesAsync(Guid userId);
}
```

**`Interfaces/Services/IUserService.cs`**
```csharp
public interface IUserService
{
    Task<UserResponse> CreateAsync(CreateUserRequest request);
    Task<UserResponse> GetByIdAsync(Guid id);
    Task<IEnumerable<UserResponse>> GetAllAsync();
}
```

### Checklist Fase 3

- [ ] Todas las interfaces usan `Task<T>` (async por defecto)
- [ ] DTOs son `record` (inmutables)
- [ ] Ninguna interfaz de repositorio tiene lógica de negocio
- [ ] `ITokenService` e `IPasswordHasher` están en Application, no en Domain (son contratos de infra)
- [ ] El proyecto Application compila solo con referencia a Domain

---

## FASE 4 — Persistence Layer

### 4.1 DbContext

**`Context/AppDbContext.cs`**
```csharp
public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
```

### 4.2 Configuraciones (Fluent API)

**`Configurations/UserConfiguration.cs`**
```csharp
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .IsRequired();

        builder.Property(u => u.Role)
            .HasConversion<string>()  // Guarda "Admin" en lugar de 3
            .HasMaxLength(50);

        builder.Property(u => u.IsDeleted)
            .HasDefaultValue(false);

        // Filtro global para soft delete
        builder.HasQueryFilter(u => !u.IsDeleted);

        builder.HasMany(u => u.RefreshTokens)
            .WithOne()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

**`Configurations/RefreshTokenConfiguration.cs`**
```csharp
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Token)
            .IsRequired()
            .HasMaxLength(512);

        builder.HasIndex(rt => rt.Token)
            .IsUnique();

        builder.Property(rt => rt.DeviceInfo)
            .HasMaxLength(512);

        builder.Property(rt => rt.IpAddress)
            .HasMaxLength(45); // IPv6 max length
    }
}
```

### 4.3 Repositorios

**`Repositories/UserRepository.cs`**
```csharp
public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context) => _context = context;

    public async Task<User?> GetByIdAsync(Guid id)
        => await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

    public async Task<User?> GetByEmailAsync(string email)
        => await _context.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());

    public async Task<bool> ExistsWithEmailAsync(string email)
        => await _context.Users.AnyAsync(u => u.Email == email.ToLowerInvariant());

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<User>> GetAllAsync()
        => await _context.Users.ToListAsync();
}
```

### 4.4 Seed Data

**`Seeds/InitialSeed.cs`**
```csharp
// NOTA: el hash se genera con BCrypt cost factor 12
// Para cambiar el admin inicial: modificar Email y ejecutar nueva migración
public static class InitialSeed
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RolePermission>().HasData(
            new { Id = 1, Role = "Admin",  PermissionsJson = """{"users":["read","create","update","delete"],"orders":["read","create","update","delete"]}""" },
            new { Id = 2, Role = "Helper", PermissionsJson = """{"users":["read"],"orders":["read","update"]}""" },
            new { Id = 3, Role = "Client", PermissionsJson = """{"orders":["read","create"]}""" }
        );
    }
}
```

> El admin se crea por endpoint o script de inicialización, NO en la migración, para evitar el problema del hash hardcodeado.

### Checklist Fase 4

- [ ] Roles se guardan como `string` en DB (no como int)
- [ ] `HasQueryFilter` para soft delete aplicado en `User`
- [ ] Índice único en `Email` y en `RefreshToken.Token`
- [ ] `Cascade delete` configurado en `RefreshToken`
- [ ] No hay lógica de negocio en los repositorios (solo queries)
- [ ] `SaveChangesAsync` solo en el repositorio, no en el service
- [ ] Las configuraciones usan Fluent API, no Data Annotations

---

## FASE 5 — Infrastructure Layer

### 5.1 Password Hasher

**`Security/BcryptPasswordHasher.cs`**
```csharp
public class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash)
        => BCrypt.Net.BCrypt.Verify(password, hash);
}
```

### 5.2 Token Service

**`Security/JwtTokenService.cs`**
```csharp
public class JwtTokenService : ITokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(IOptions<JwtSettings> settings)
        => _settings = settings.Value;

    public string GenerateJwt(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_settings.Secret));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("token_version", user.TokenVersion.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.ExpirationMinutes),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}
```

**`Security/JwtSettings.cs`**
```csharp
public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
```

### Checklist Fase 5

- [ ] BCrypt con work factor 12 (mínimo aceptable para producción)
- [ ] `GenerateRefreshToken` usa `RandomNumberGenerator`, no `Random`
- [ ] JWT incluye `token_version` claim
- [ ] JWT incluye `jti` (JWT ID) para futura revocación por token
- [ ] `JwtSettings` se popula desde configuración (no hardcodeada)

---

## FASE 6 — Application Services

### 6.1 AuthService

**`Services/AuthService.cs`** — Esqueleto con puntos clave:

```csharp
public class AuthService : IAuthService
{
    // Inyectar: IUserRepository, IRefreshTokenRepository,
    //           IPasswordHasher, ITokenService, JwtSettings

    public async Task<LoginResponse> LoginAsync(
        LoginRequest request, string? deviceInfo, string? ipAddress)
    {
        // 1. Buscar usuario por email
        // 2. Verificar que existe y está activo
        // 3. Verificar password con IPasswordHasher
        // 4. Registrar último login
        // 5. Generar JWT
        // 6. Generar refresh token y guardarlo
        // 7. Retornar LoginResponse
    }

    public async Task<LoginResponse> RefreshAsync(
        string refreshToken, string? deviceInfo, string? ipAddress)
    {
        // 1. Buscar refresh token en DB
        // 2. Verificar IsValid (no revocado, no expirado)
        // 3. Buscar usuario asociado
        // 4. Revocar el refresh token usado
        // 5. Generar nuevo JWT
        // 6. Generar nuevo refresh token (rotación)
        // 7. Guardar nuevo refresh token
        // 8. Retornar LoginResponse
    }

    public async Task LogoutAsync(string refreshToken)
    {
        // 1. Buscar refresh token
        // 2. Si existe, revocarlo
        // (no lanzar error si no existe — idempotente)
    }

    public async Task LogoutAllDevicesAsync(Guid userId)
    {
        // 1. Revocar todos los refresh tokens del usuario
        // 2. IncrementTokenVersion en el usuario
        // (invalida también los JWT existentes via token_version)
    }
}
```

### Checklist Fase 6

- [ ] Login retorna error genérico si email o password son incorrectos (no especificar cuál falla)
- [ ] Refresh es one-time-use (revocar el token usado antes de emitir nuevo)
- [ ] Logout es idempotente (no falla si el token ya estaba revocado)
- [ ] LogoutAll incrementa TokenVersion (invalida JWT aunque no hayan expirado)
- [ ] Services no tienen referencia directa a EF ni a HttpContext

---

## FASE 7 — API Layer

### 7.1 Middleware de manejo de errores

**`Middleware/ExceptionMiddleware.cs`**
```csharp
// Captura todas las excepciones no manejadas
// Retorna ProblemDetails (RFC 7807)
// DomainException → 400
// UnauthorizedException → 401
// Exception genérica → 500 (sin exponer detalles internos)
```

### 7.2 Middleware de validación de TokenVersion

**`Middleware/TokenVersionMiddleware.cs`**
```csharp
// En cada request autenticado:
// 1. Extraer token_version del JWT
// 2. Buscar TokenVersion actual del usuario en DB
// 3. Si difieren → 401 Unauthorized
// NOTA: Considerar cache (IMemoryCache) para no golpear DB en cada request
```

### 7.3 Controllers

**Regla:** Solo reciben request, llaman al service, retornan resultado.

```csharp
[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var deviceInfo = Request.Headers["User-Agent"].ToString();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.LoginAsync(request, deviceInfo, ipAddress);
        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request) { ... }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request) { ... }

    [HttpPost("logout-all")]
    [Authorize]
    public async Task<IActionResult> LogoutAll()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        await _authService.LogoutAllDevicesAsync(userId);
        return NoContent();
    }
}
```

### 7.4 Registro de dependencias

**`Extensions/ServiceCollectionExtensions.cs`**
```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        return services;
    }

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        services.Configure<JwtSettings>(config.GetSection("JwtSettings"));
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        return services;
    }

    public static IServiceCollection AddPersistence(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(config.GetConnectionString("Default")));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        return services;
    }
}
```

### Checklist Fase 7

- [ ] ExceptionMiddleware registrado como primer middleware
- [ ] No hay try/catch en los controllers
- [ ] Device info e IP se capturan en el controller, no en el service
- [ ] `logout-all` valida que el userId viene del JWT, no del body
- [ ] Swagger configurado con Bearer token
- [ ] Rate limiting configurado en `/auth/login` y `/auth/refresh`

---

## FASE 8 — Configuración y Seguridad Final

### 8.1 appsettings.json (sin secretos)

```json
{
  "JwtSettings": {
    "Issuer": "AuthProject",
    "Audience": "AuthProject",
    "ExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "RateLimiting": {
    "LoginMaxAttempts": 5,
    "LoginWindowSeconds": 300
  }
}
```

### 8.2 appsettings.Development.json (en .gitignore)

```json
{
  "ConnectionStrings": {
    "Default": "Server=...;Database=AuthProject;..."
  },
  "JwtSettings": {
    "Secret": "TU_SECRET_MINIMO_32_CARACTERES_AQUI_CAMBIALO"
  }
}
```

### 8.3 Validaciones de startup

```csharp
// En Program.cs — si falta el secret, la app no inicia
var jwtSecret = builder.Configuration["JwtSettings:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
    throw new InvalidOperationException(
        "JWT Secret is missing or too short. Set JwtSettings:Secret (min 32 chars).");
```

### Checklist Fase 8

- [ ] `.gitignore` cubre `appsettings.Development.json` y `appsettings.Production.json`
- [ ] El secret tiene mínimo 32 caracteres
- [ ] La app falla en startup si falta el secret (fail fast)
- [ ] Rate limiting activo en endpoints de auth
- [ ] HTTPS forzado en producción (`app.UseHttpsRedirection()`)
- [ ] CORS configurado explícitamente (no `AllowAnyOrigin` en producción)

---

## FASE 9 — Migración Inicial

```bash
# Desde la carpeta raíz
dotnet ef migrations add InitialCreate \
  --project AuthProject.Persistence \
  --startup-project AuthProject.Api

dotnet ef database update \
  --project AuthProject.Persistence \
  --startup-project AuthProject.Api
```

### Script de inicialización del admin

> Crear un endpoint protegido o un comando CLI para crear el primer admin.
> NO hardcodear el hash en la migración.

```bash
POST /setup/admin
Body: { "email": "admin@tudominio.com", "password": "..." }
# Solo funciona si no existe ningún usuario con rol Admin
# Deshabilitar o eliminar este endpoint después del primer uso
```

### Checklist Fase 9

- [ ] Migración generada sin errores
- [ ] Tablas creadas con los campos correctos
- [ ] Índices presentes (`Email`, `RefreshToken.Token`)
- [ ] Seed de permisos aplicado
- [ ] Admin creado via script, no en migración

---

## FASE 10 — Pruebas Manuales con Swagger

### Flujo completo a verificar

```
1. POST /users          → crear usuario con rol Client
2. POST /auth/login     → obtener JWT + RefreshToken
3. GET  /users/me       → con JWT válido (debe funcionar)
4. Esperar expiración   → GET /users/me (debe dar 401)
5. POST /auth/refresh   → obtener nuevo JWT + RefreshToken
6. POST /auth/refresh   → usar el refresh anterior (debe dar 401 — one-time-use)
7. POST /auth/logout    → revocar refresh actual
8. POST /auth/refresh   → intentar usar el revocado (debe dar 401)
9. POST /auth/login     → 6 intentos rápidos (debe dar 429 en el 6to)
10. POST /auth/logout-all → desde otro dispositivo (todos los refresh revocados)
```

### Checklist Fase 10

- [ ] Todos los flujos del punto anterior funcionan correctamente
- [ ] JWT expirado da 401, no 500
- [ ] Refresh one-time-use verificado
- [ ] Rate limiting verificado
- [ ] Soft delete verificado (usuario eliminado no puede hacer login)
- [ ] TokenVersion verificado (LogoutAll invalida JWT aunque no haya expirado)

---

## Resumen de Decisiones

| # | Decisión | Elegida | Motivo |
|---|----------|---------|--------|
| 0.1 | Tipo de PK | `Guid` | Mayor seguridad, no enumerable |
| 0.2 | Permisos por | Rol (JSON) | Centralizado, cambio afecta a todos del rol |
| 0.3 | Claims en JWT | `sub` + `role` | Simple ahora, escalable en v2 |
| 0.4 | Multi-dispositivo | Múltiples refresh tokens | Flexibilidad + futuro device manager |
| 0.5 | Rate limiting | Middleware ASP.NET | Autocontenido, sin dependencia de infra |

---

## Estado del Proyecto

| Fase | Estado | Notas |
|------|--------|-------|
| 0 — Decisiones | ✅ Completada | Guid, Permisos x Rol, sub+role en JWT, multi-device, middleware |
| 1 — Estructura | ✅ Completada | Solución compila limpia, paquetes net9.0, carpetas listas |
| 2 — Domain | ✅ Completada | 5 archivos, 0 warnings, sin dependencias externas |
| 3 — Application | ✅ Completada | 11 archivos, 0 warnings, interfaces en Interfaces/Services/ |
| 4 — Persistence | ✅ Completada | DbContext, configs, repositorios, seed de permisos |
| 5 — Infrastructure | ✅ Completada | BCrypt w12, JWT con token_version+jti, RefreshToken con CSPRNG |
| 6 — App Services | ✅ Completada | AuthService + UserService, lógica de negocio completa |
| 7 — API Layer | ⬜ Pendiente | |
| 8 — Seguridad | ⬜ Pendiente | |
| 9 — Migraciones | ⬜ Pendiente | |
| 10 — Pruebas | ⬜ Pendiente | |
