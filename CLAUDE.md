# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

Firmeza is an administrative panel for a construction materials sales business. It is built with ASP.NET Core Razor Pages (.NET 10), Entity Framework Core, and PostgreSQL. Authentication uses ASP.NET Core Identity with two roles: `Administrador` (full panel access) and `Cliente` (reserved for future external integrations).

## Commands

### Run locally (requires PostgreSQL on localhost:5432)
```bash
cd src/Firmeza.Web
dotnet run
# Available at http://localhost:5200
```

### Apply database migrations
```bash
cd src/Firmeza.Web
dotnet ef database update
```

### Add a new migration
```bash
cd src/Firmeza.Web
dotnet ef migrations add <NombreMigracion>
```

### Run tests
```bash
dotnet test tests/Firmeza.UnitTests
# or to run a single test file:
cd tests/Firmeza.UnitTests
dotnet test --filter "FullyQualifiedName~SaleDetailTests"
```

### Docker (no local PostgreSQL needed)
```bash
docker compose build
docker compose up
# Available at http://localhost:8080
```

## Architecture

### Data flow pattern
All Razor pages access the database directly via `ApplicationDbContext` injected into the page model. There is no service layer — EF Core queries live in `OnGetAsync` / `OnPostAsync` handlers. ViewModels carry data annotations for form validation and are mapped to/from domain models in page model handlers.

### Key layers
- `src/Firmeza.Web/Models/` — EF Core entities: `Product`, `Client`, `Sale`, `SaleDetail`. `SaleDetail.Subtotal` is a computed property (`Cantidad * PrecioUnitario`), not stored.
- `src/Firmeza.Web/ViewModels/` — Form-bound models with localized validation messages (Spanish). Always prefer ViewModels for form POST handlers; use raw entities for read-only display.
- `src/Firmeza.Web/Data/ApplicationDbContext.cs` — inherits `IdentityDbContext`, exposes `Productos`, `Clientes`, `Ventas`, `DetalleVentas` DbSets. Note: DbSet names are Spanish while entity class names are English.
- `src/Firmeza.Web/Data/SeedData.cs` — runs at startup to create roles (`Administrador`, `Cliente`) and the default admin user (`admin@firmeza.com` / `Admin123!`).
- `src/Firmeza.Web/Pages/` — organized by entity (Productos, Clientes). Index pages support search and filtering via `[BindProperty(SupportsGet = true)]` query parameters.
- `src/Firmeza.Web/Areas/Identity/` — scaffolded Identity pages for Login, Register, Logout.
- `tests/Firmeza.UnitTests/` — xUnit tests; currently only covers `SaleDetail.Subtotal` computed property.

### Migrations
Migrations live in `src/Firmeza.Web/Migrations/`. Run `dotnet ef` commands from `src/Firmeza.Web/` so the CLI picks up the correct project context.

## Connection string
Local dev connection string is set in `src/Firmeza.Web/appsettings.json`. Docker overrides it via the `ConnectionStrings__DefaultConnection` environment variable in `compose.yaml` (password: `firmeza123`, host: `db`).
