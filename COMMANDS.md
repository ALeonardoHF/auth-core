# Comandos del Proyecto

## API

```bash
# Correr la API
dotnet run --project AuthProject.Api

# Correr en modo watch (reinicia al guardar)
dotnet watch --project AuthProject.Api
```

## Tests

```bash
# Correr todos los tests
dotnet test

# Correr con detalles
dotnet test --verbosity normal

# Correr solo un proyecto de tests
dotnet test AuthProject.Tests
```

## Base de Datos (EF Core)

```bash
# Crear una migración nueva
dotnet ef migrations add NombreDeLaMigracion --project AuthProject.Persistence --startup-project AuthProject.Api

# Aplicar migraciones
dotnet ef database update --project AuthProject.Persistence --startup-project AuthProject.Api

# Revertir última migración
dotnet ef migrations remove --project AuthProject.Persistence --startup-project AuthProject.Api
```

## NuGet

```bash
# Agregar paquete a un proyecto
dotnet add NombreDelProyecto package NombreDelPaquete

# Restaurar paquetes
dotnet restore
```

## Docker

```bash
# Levantar todos los servicios (SQL Server + API)
docker compose up -d

# Ver logs de la API
docker compose logs api -f

# Detener todo
docker compose down

# Reconstruir imagen de la API
docker compose build api

# Reconstruir y levantar
docker compose up -d --build
```

## SQL Server en Docker

```bash
# Ver nombre del contenedor de la BD
docker ps

# Entrar al SQL Server de Docker
docker exec -it login-db-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Admin123!" -C

# Ver logs de la BD
docker compose logs db --tail=50
```

## SQL útiles

```sql
-- Ver tokens de confirmación de email
SELECT * FROM EmailConfirmationTokens ORDER BY CreatedAt DESC

-- Ver usuarios
SELECT Id, Email, Role, IsActive, IsEmailConfirmed FROM Users

-- Ver audit logs
SELECT * FROM AuditLogs ORDER BY CreatedAt DESC

-- Ver refresh tokens
SELECT * FROM RefreshTokens ORDER BY CreatedAt DESC
```

## Endpoints principales

```
POST   /users                          Registro de usuario
GET    /users/me                       Datos del usuario autenticado (JWT)
GET    /users                          Todos los usuarios (Admin)
POST   /auth/login                     Login → { requiresTwoFactor, email, auth }
POST   /auth/refresh                   Renovar tokens
POST   /auth/logout                    Logout (JWT)
POST   /auth/logout-all                Logout todos los dispositivos (JWT)
GET    /auth/confirm-email?token=XYZ   Confirmar email
POST   /auth/forgot-password           Solicitar reset de contraseña
POST   /auth/reset-password            Resetear contraseña con token
GET    /auth/reset-password?token=XYZ  Formulario HTML para reset
POST   /auth/2fa/setup                 Activar 2FA → QR + manualCode (JWT)
GET    /auth/2fa/setup-page            Página HTML con QR (JWT)
POST   /auth/2fa/verify                Verificar código TOTP → tokens
GET    /health                         Health check
POST   /auth/2fa/disable                Desactivar 2FA (JWT + código TOTP)
POST   /auth/2fa/recovery/request       Solicitar recovery por email
POST   /auth/2fa/recovery/confirm       Confirmar recovery (token + password)
```

## ngrok (tunnel para pruebas móvil/externas)

```bash
ngrok http 5000
```
