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
POST   /users                          Crear usuario
GET    /users/{id}                     Obtener usuario
GET    /users                          Obtener todos los usuarios
POST   /auth/login                     Login
POST   /auth/refresh                   Renovar token
POST   /auth/logout                    Logout
GET    /auth/confirm-email?token=XYZ   Confirmar email
GET    /health                         Health check
```

## ngrok (tunnel para pruebas móvil/externas)

```bash
ngrok http 5000
```
