# 🌿 NutriCasa · Nutrición adaptada a tu vida

Plataforma PWA + API de coaching nutricional cetogénico para grupos familiares, con adaptación dinámica vía Gemini y personalización por presupuesto y estilo de vida.

## Stack tecnológico

| Capa | Tecnología | Versión |
|------|------------|---------|
| Runtime | .NET | 8 (LTS) |
| API | ASP.NET Core Web API | 8 |
| ORM | Entity Framework Core | 8 |
| BD | PostgreSQL (Neon) | 15+ |
| CQRS | MediatR | 12 |
| Validación | FluentValidation | 11 |
| Mapping | Mapster | 7 |
| Logging | Serilog | 8 |
| Auth | JWT + BCrypt | 8 |
| Background | Hangfire | 1.8 |
| Testing | xUnit + FluentAssertions + Moq | latest |

## Prerrequisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) o superior
- PostgreSQL 15+ ([Neon](https://neon.tech) o Docker local)
- Git

## Setup paso a paso

### 1. Clonar

```bash
git clone <tu-repo-url>
cd NutriCasa
```

### 2. Configurar connection string

```bash
cd src/NutriCasa.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=<tu-host>;Database=nutricasa;Username=<user>;Password=<pass>;SSL Mode=Require;Trust Server Certificate=true"
```

### 3. Aplicar migraciones

```bash
dotnet ef database update --project src/NutriCasa.Infrastructure --startup-project src/NutriCasa.Api
```

### 4. Ejecutar

```bash
dotnet run --project src/NutriCasa.Api
```

### 5. Alternativa Docker (PostgreSQL local)

```bash
docker-compose up -d
# Connection string local:
# Host=localhost;Port=5433;Database=nutricasa;Username=nutricasa;Password=CHANGE-ME
```

## URLs

| Recurso | URL |
|---------|-----|
| Swagger | `https://localhost:7120/swagger` |
| Hangfire | `https://localhost:7120/hangfire` |
| Health | `https://localhost:7120/api/health` |

## Estructura del proyecto

```
NutriCasa/
├── NutriCasa.sln
├── Directory.Build.props
├── docker-compose.yml
├── src/
│   ├── NutriCasa.Domain/          # Entidades, enums, value objects
│   ├── NutriCasa.Application/     # Interfaces, behaviors, CQRS
│   ├── NutriCasa.Infrastructure/  # EF Core, servicios, seeds
│   └── NutriCasa.Api/             # Controllers, middleware, config
└── tests/
    ├── NutriCasa.Domain.Tests/
    ├── NutriCasa.Application.Tests/
    └── NutriCasa.Api.Tests/
```

## Convenciones

- **Código**: PascalCase en C#, snake_case automático en BD via `EFCore.NamingConventions`
- **Idioma de código**: inglés (`User`, `GeneratePlanCommand`)
- **Idioma de mensajes**: español MX ("Usuario no encontrado")
- **Commits**: Conventional Commits (`feat:`, `fix:`, `chore:`)
- **Nullable reference types**: habilitado, zero warnings

## Roadmap

| Fase | Descripción | Estado |
|------|-------------|--------|
| 0 | Schema + Clean Architecture + Seeds | ✅ Actual |
| 1 | Auth + Registro + Perfil médico + Planes IA | Pendiente |
| 1.5 | Recetas curated + catálogo inicial | Pendiente |
| 2 | Editor drag-and-drop + Swaps | Pendiente |
| 3 | Fotos progreso + R2 Storage | Pendiente |
| 4 | Social + Moderación | Pendiente |
| 5 | Gamificación + Challenges | Pendiente |
| 6 | Chat IA + SignalR | Pendiente |
| 7 | PWA + Push notifications | Pendiente |
| 8 | Monetización + MercadoPago | Pendiente |

## Servicios stub preparados

Los siguientes servicios están registrados como stubs con `NotImplementedException`, listos para implementarse en fases posteriores:

- `GeminiServiceStub` → Fase 1: Integración con Gemini 2.5 Pro/Flash
- `PlanValidatorStub` → Fase 1: Validador de respuestas IA
- `CostEstimationServiceStub` → Fase 1: Estimación de costos
- `IngredientSubstitutionServiceStub` → Fase 1: Sustitución por modo
- `CloudflareR2StorageService` → Fase 3: Storage de fotos
- `ModerationServiceStub` → Fase 4: Filtro híbrido
- `MercadoPagoServiceStub` → Fase 8: Pagos

## Licencia

Privado · © 2026 NutriCasa
