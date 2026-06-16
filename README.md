# Firmeza — Panel Administrativo

Sistema de gestión para un negocio de venta y distribución de materiales de construcción. Permite administrar productos, clientes y ventas a través de un panel administrativo desarrollado en ASP.NET Core (Razor Pages).

## Características

- Panel administrativo con Bootstrap 5 (sidebar con grupos, hover en tablas, formularios responsivos)
- Autenticación y autorización con ASP.NET Core Identity (roles: Administrador / Cliente)
- CRUD completo de Productos, Clientes y Ventas con validaciones en español
- Búsqueda y filtrado en los listados; filtro de estado en Ventas
- Carga masiva de datos desnormalizados desde Excel (.xlsx) con EPPlus — soporta productos, clientes y ventas en un mismo archivo
- Exportación de Productos, Clientes y Ventas a Excel (.xlsx) y PDF (Productos y Clientes)
- Generación automática de recibo PDF al registrar una venta (almacenado en `wwwroot/recibos/`, descargable desde Detalles)
- Dashboard con contadores y tabla de últimas 5 ventas
- 38 pruebas unitarias con xUnit
- Persistencia en PostgreSQL mediante Entity Framework Core (migraciones)
- Entorno preparado para despliegue con Docker (volumen persistente para recibos PDF)

## Tecnologías

- .NET 10 / ASP.NET Core (Razor Pages)
- Entity Framework Core + Npgsql
- PostgreSQL 16
- ASP.NET Core Identity
- Bootstrap 5
- EPPlus 8 (lectura/escritura de Excel)
- QuestPDF 2026.6 (generación de PDF)
- xUnit (pruebas unitarias)
- Docker / Docker Compose

## Estructura del proyecto

```
firmeza/
├── compose.yaml
├── .dockerignore
├── docs/
│   ├── diagramas.md          # Diagramas de clases en Mermaid
│   ├── Clases.png
│   └── EDR.png
├── src/
│   └── Firmeza.Web/
│       ├── Data/             # DbContext (ApplicationDbContext) y SeedData
│       ├── Models/           # Entidades: Product, Client, Sale, SaleDetail
│       ├── ViewModels/       # SaleViewModel, CargaMasivaViewModel, etc.
│       ├── Validation/       # ValidacionCargaMasiva (lógica extraída, usada en tests)
│       ├── Pages/
│       │   ├── Productos/    # CRUD + exportación Excel/PDF
│       │   ├── Clientes/     # CRUD + exportación Excel/PDF
│       │   ├── Ventas/       # CRUD + generación de recibo PDF
│       │   ├── CargaMasiva/  # Carga masiva desde Excel
│       │   └── Shared/       # _Layout.cshtml, _LoginPartial
│       ├── Areas/Identity/   # Páginas de autenticación (Login, Register, Logout)
│       ├── Migrations/       # Migraciones de EF Core
│       └── wwwroot/
│           └── recibos/      # Recibos PDF generados en runtime (no en repo)
├── tests/
│   └── Firmeza.UnitTests/
│       ├── SaleDetailTests.cs           # Subtotal calculado
│       ├── SaleTests.cs                 # Totales de venta e IVA
│       └── CargaMasivaValidacionTests.cs # Reglas de validación del Excel
└── tools/
    └── GenerarExcelPrueba/   # Consola que genera datos_prueba.xlsx de muestra
```

## Modelo de datos

Ver diagrama entidad-relación en `docs/EDR.png` y diagrama de clases en `docs/diagramas.md`.

Entidades principales:

- **Product** (Productos): nombre, descripción, precio, stock, categoría, unidad de medida.
- **Client** (Clientes): nombre, documento, email, teléfono, dirección, edad.
- **Sale** (Ventas): fecha, cliente, total, estado (`Pendiente` / `Pagada` / `Anulada`).
- **SaleDetail** (Detalle de venta): producto, cantidad, precio unitario. `Subtotal` es propiedad calculada (`Cantidad × PrecioUnitario`), no almacenada.

## Roles del sistema

- **Administrador**: acceso completo al panel (Productos, Clientes, Ventas, Dashboard, Carga Masiva).
- **Cliente**: cuenta registrada, sin acceso al panel (reservado para futuras integraciones externas).

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

5. Abrir el navegador en `http://localhost:5200`.

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

3. PostgreSQL quedará disponible en `localhost:5432` (usuario `postgres`, contraseña `firmeza123`, base de datos `firmeza_db`).

> Los recibos PDF se almacenan en el volumen `firmeza_recibos` y persisten entre reinicios del contenedor.

> Las migraciones deben aplicarse manualmente la primera vez: `dotnet ef database update` desde el host apuntando a `localhost:5432`.

## Carga masiva desde Excel

La página **Carga Masiva** (`/CargaMasiva`) acepta un archivo `.xlsx` con datos desnormalizados. Una misma fila puede contener datos de Producto, Cliente y Venta simultáneamente; el sistema detecta qué bloque activar por la presencia de columnas clave.

### Columnas reconocidas

| Bloque | Columna disparadora | Columnas |
|--------|-------------------|----------|
| **Producto** | `NombreProducto` | NombreProducto `*`, DescripcionProducto, PrecioProducto `*`, StockProducto `*`, CategoriaProducto, UnidadMedida |
| **Cliente** | `Documento` | NombreCliente `*`, Documento `*`, EmailCliente, TelefonoCliente, DireccionCliente, Edad `*` |
| **Venta/Detalle** | `FechaVenta` | DocumentoCliente `*`, FechaVenta `*`, NombreProductoVenta `*`, CantidadVenta `*`, PrecioUnitarioVenta `*` |

`*` = campo obligatorio para ese bloque.

### Comportamiento

- **Dos pasadas**: primero se procesan todos los productos y clientes del archivo (en cualquier fila), luego se procesan las ventas. Esto permite que la venta referencie un cliente/producto definido más abajo en el mismo archivo.
- **Errores no fatales por fila**: si una fila tiene datos inválidos (precio negativo, edad fuera de rango 0-120, stock negativo, referencia a producto/cliente no encontrado), se registra el error y se continúa con la siguiente fila. Al final se muestra un resumen con el conteo de filas procesadas y la lista de errores.
- **Error fatal**: si el archivo no es un `.xlsx` válido o está vacío, se aborta y se muestra el error sin procesar nada.
- **Upsert**: si el producto (por nombre) o el cliente (por documento) ya existe, se actualizan sus datos; si no existe, se crea.

### Generar un archivo de prueba

```bash
dotnet run --project tools/GenerarExcelPrueba
```

Genera `datos_prueba.xlsx` en el directorio de ejecución con 2 productos válidos, 1 inválido (precio = -1), 2 clientes válidos, 1 inválido (edad = 200) y 2 filas de venta.

## Exportaciones y recibos PDF

### Exportación de listados

Los botones de exportación están en los listados principales:

| Página | Formatos disponibles |
|--------|---------------------|
| Productos (`/Productos`) | Excel + PDF |
| Clientes (`/Clientes`) | Excel + PDF |
| Ventas (`/Ventas`) | Excel |

Los botones aparecen como un menú desplegable **"⬇ Exportar"** en Productos y Clientes. La exportación preserva los filtros activos en el listado.

- **Excel** generado con EPPlus: encabezados con estilo, columnas auto-ajustadas, formato numérico en precios.
- **PDF** generado con QuestPDF: tabla con alternancia de color por fila, encabezado con nombre y fecha de generación, pie de página con número de página.

### Recibos de venta (PDF automático)

Al guardar una venta nueva, el sistema genera automáticamente un recibo PDF en `wwwroot/recibos/recibo-{id}.pdf`. El recibo incluye:

- Número y fecha de la venta
- Datos del cliente (nombre, documento, email, teléfono)
- Tabla de productos con cantidad, precio unitario y subtotal por línea
- Subtotal, IVA (19%) y total final
- Pie de página "Gracias por su compra"

Si la generación del PDF falla por algún motivo, la venta se guarda de todas formas (el PDF es no-crítico). El recibo se puede descargar desde la página **Detalles** de la venta con el botón **"⬇ Descargar Recibo"** (solo aparece si el archivo existe).

## Pruebas

El proyecto incluye 38 pruebas unitarias con xUnit en `tests/Firmeza.UnitTests/`.

```bash
dotnet test tests/Firmeza.UnitTests
```

| Archivo | Qué prueba |
|---------|------------|
| `SaleDetailTests.cs` | Cálculo de `Subtotal` (Cantidad × PrecioUnitario) |
| `SaleTests.cs` | Suma de subtotales en la venta, venta sin detalles = 0, cálculo de IVA al 19% |
| `CargaMasivaValidacionTests.cs` | `PrecioEsValido`, `EdadEsValida`, `TryParseEntero`, `ParseEnteroEstricto` (FormatException) |
