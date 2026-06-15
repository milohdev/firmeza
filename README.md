# Firmeza — Módulo Administrativo Base

Sistema de gestión para un negocio de venta y distribución de materiales de construcción. Permite administrar productos, clientes y ventas a través de un panel administrativo desarrollado en ASP.NET Core (Razor Pages).

## Características

- Panel administrativo con Bootstrap 5
- Autenticación y autorización con ASP.NET Core Identity (roles: Administrador / Cliente)
- CRUD completo de Productos y Clientes con validaciones
- Búsqueda y filtrado en los listados
- Persistencia en PostgreSQL mediante Entity Framework Core (migraciones)
- Entorno preparado para despliegue con Docker

## Tecnologías

- .NET 10 / ASP.NET Core (Razor Pages)
- Entity Framework Core + Npgsql
- PostgreSQL 16
- ASP.NET Core Identity
- Bootstrap 5
- Docker / Docker Compose

## Estructura del proyecto

firmeza/

├── Firmeza.slnx

├── docker-compose.yml

├── src/

│   └── Firmeza.Web/        # Proyecto Razor Pages

│       ├── Data/             # DbContext y Seed

│       ├── Models/           # Entidades de dominio

│       ├── ViewModels/       # Modelos de vista con validaciones

│       ├── Pages/             # Páginas Razor (Productos, Clientes, etc.)

│       ├── Areas/Identity/    # Páginas de autenticación

│       └── Migrations/        # Migraciones de EF Core

└── tests/


## Modelo de datos

Ver diagrama entidad-relación en `docs/er-diagram.png`.

Entidades principales:

- **Product** (Productos): nombre, descripción, precio, stock, categoría, unidad de medida.
- **Client** (Clientes): nombre, documento, email, teléfono, dirección, edad.
- **Sale** (Ventas): fecha, cliente, total, estado.
- **SaleDetail** (Detalle de venta): producto, cantidad, precio unitario.

## Roles del sistema

- **Administrador**: acceso completo al panel Razor (Productos, Clientes, Ventas, Dashboard).
- **Cliente**: cuenta registrada en el sistema, sin acceso al panel Razor (reservado para futuras integraciones con apps externas).

## Instalación y ejecución — Local

### Requisitos

- .NET SDK 10.0
- PostgreSQL 16 (corriendo en `localhost:5432`)

### Pasos

1. Clonar el repositorio:
```bash
   git clone <url-del-repo>
   cd firmeza
```

2. Configurar la cadena de conexión en `src/Firmeza.Web/appsettings.json`:
```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=firmeza_db;Username=postgres;Password=TU_PASSWORD"
   }
```

3. Aplicar migraciones:
```bash
   cd src/Firmeza.Web
   dotnet ef database update
```

4. Ejecutar la aplicación:
```bash
   dotnet run
```

5. Abrir el navegador en la URL indicada (por defecto `http://localhost:5200`).

### Usuario administrador inicial (seed)

- **Email**: `admin@firmeza.com`
- **Contraseña**: `Admin123!`

## Instalación y ejecución — Docker

### Requisitos

- Docker y Docker Compose

### Pasos

1. Desde la raíz del proyecto:
```bash
   docker compose build
   docker compose up
```

2. La aplicación quedará disponible en `http://localhost:8080`.

3. PostgreSQL quedará disponible en `localhost:5432` (usuario `postgres`, contraseña `postgres`, base de datos `firmeza_db`).

> Nota: las migraciones deben aplicarse manualmente la primera vez, ejecutando `dotnet ef database update` desde el host (apuntando a `localhost:5432`) o configurando la aplicación para aplicar migraciones automáticamente al iniciar.

## Pruebas

El proyecto incluye pruebas unitarias básicas con xUnit en `tests/Firmeza.UnitTests/`.

```bash
cd tests/Firmeza.UnitTests
dotnet test
```