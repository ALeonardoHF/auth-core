# Roadmap — AuthProject

## Lo que ya tienes listo ✅

- [x] JWT + Refresh rotation con TokenVersion
- [x] BCrypt work factor 12
- [x] Rate limiting por IP
- [x] JWT secret en variable de entorno
- [x] Lockout por email (5 intentos → bloqueo 15 min)
- [x] Logs de auditoría en BD (AuditLogs)
- [x] 18 tests cubriendo escenarios críticos

---

## Nivel 1 — Obligatorio antes de deployar

| Estado | Qué | Por qué |
|--------|-----|---------|
| ✅ | Logs de auditoría | Saber quién entró, cuándo y desde dónde |
| ⬜ | HTTPS forzado | Necesitas certificado real en producción |
| ✅ | CORS configurado | Definir qué dominios pueden consumir la API |
| ✅ | Health check | `GET /health` para saber si la app está viva |

---

## Nivel 2 — Importante para producción real

| Estado | Qué | Por qué |
|--------|-----|---------|
| ✅ | Docker | Deployar igual en cualquier servidor |
| ⬜ | Variables de entorno en servidor | Connection string y JWT secret fuera del código |
| ⬜ | Backups de BD | Si se cae SQL Server, no pierdes datos |

---

## Nivel 3 — Siguiente evolución

| Estado | Qué | Por qué |
|--------|-----|---------|
| ✅ | Confirmación de email | Verificar que el email existe antes de activar cuenta |
| ✅ | Reset de password | Flujo estándar via email |
| ⬜ | 2FA | Segunda capa de seguridad |
| ⬜ | Monitoreo de errores | Sentry — te avisa cuando algo explota en prod |
| ✅ | CI/CD | GitHub Actions que corre los tests antes de deployar |

---

## Siguiente paso

```
1. ⬜ CORS
2. ⬜ Health check
3. ⬜ Docker
4. ⬜ CI/CD con GitHub Actions
```

---

## Ideas de proyectos futuros que consumen esta API

### 1. App de manejo de gastos
- Frontend en **React**
- API de negocio en **Node.js** (endpoints propios)
- Auth delegada a este proyecto
- Features: registro de gastos, categorías, recordatorios, notificaciones push de pagos próximos

**Stack sugerido:**
```
React → Node.js API → AuthProject (login/tokens)
                    → BD propia (gastos, categorías, recordatorios)
```

### 2. Landing + Admin para carpintería familiar
- Landing pública con portafolio de trabajos
- Panel admin para gestionar contenido
- Frontend en **Blazor (ASP.NET Core)**
- Auth delegada a este proyecto como servicio externo

**Stack sugerido:**
```
Blazor → AuthProject API (login externo)
       → BD propia (productos, proyectos, galería)
```

---

> Ambos proyectos pueden reutilizar este AuthProject sin modificarlo —
> solo consumen `/auth/login`, `/auth/refresh` y `/auth/logout`.
