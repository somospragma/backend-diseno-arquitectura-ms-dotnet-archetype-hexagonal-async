# Arquetipo .NET 10 — API hexagonal async

Microservicio de cuentas sobre .NET 10 (LTS) con arquitectura hexagonal y async/await.
Para lecturas en streaming y eventos en vivo usar el arquetipo `dotnet-api-streams`.

> Documentacion completa (ADRs, threat model, runbook) pendiente con la plantilla de Alejandria.

## Estructura

```
src/
  domain/          Model, Ports, UseCases (sin frameworks)
  infrastructure/
    driven-adapters/  Persistence (EF Core + PostgreSQL), Cache (Redis), Messaging (SQS), CreditScore (HTTP + resiliencia)
    entry-points/     Api (Minimal APIs, RFC 7807)
    helpers/          Correlation id, security headers, enmascaramiento
  application/     Composition root (Program.cs)
tests/             Unitarios, integracion, arquitectura (ArchUnitNET)
```

## Comandos

```bash
dotnet build Pragma.Accounts.slnx                              # warnings como errores + NuGetAudit
dotnet test --solution Pragma.Accounts.slnx --coverlet         # tests + gate de cobertura 90%
dotnet format Pragma.Accounts.slnx --verify-no-changes         # estilo
dotnet tool restore && (cd tests/Pragma.Accounts.Tests && dotnet stryker)   # mutation testing (60%)
docker build -t pragma-accounts .
```

Configuracion por variables de entorno: ver `.env.example`.

## Endpoints

| Metodo | Ruta | Descripcion |
|---|---|---|
| GET | `/api/v1/accounts?limit=20` | Listado |
| GET | `/api/v1/accounts/{id}` | Detalle |
| POST | `/api/v1/accounts` | Crear |
| PUT | `/api/v1/accounts/{id}` | Reemplazar |
| PATCH | `/api/v1/accounts/{id}` | Actualizacion parcial |
| DELETE | `/api/v1/accounts/{id}` | Eliminar |
| QUERY | `/api/v1/accounts/search` | Busqueda con body |
| GET | `/api/v1/accounts/{id}/credit-score` | Puntaje crediticio |
| GET | `/health/live`, `/health/ready` | Health checks |
