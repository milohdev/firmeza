# Diagramas de Firmeza

## Diagrama de clases

```mermaid
classDiagram
    direction LR

    class Product {
        +int Id
        +string Nombre
        +string? Descripcion
        +decimal Precio
        +int Stock
        +string? Categoria
        +string UnidadMedida
    }

    class Client {
        +int Id
        +string Nombre
        +string Documento
        +string? Email
        +string? Telefono
        +string? Direccion
        +int Edad
    }

    class Sale {
        +int Id
        +DateTime Fecha
        +int ClienteId
        +decimal Total
        +SaleStatus Estado
        +Client? Cliente
        +ICollection~SaleDetail~ Detalles
    }

    class SaleDetail {
        +int Id
        +int VentaId
        +int ProductoId
        +int Cantidad
        +decimal PrecioUnitario
        +decimal Subtotal
        +Product? Producto
    }

    class SaleStatus {
        <<enumeration>>
        Pendiente
        Pagada
        Anulada
    }

    class ValidacionCargaMasiva {
        <<utility>>
        +PrecioEsValido(decimal) bool$
        +EdadEsValida(int) bool$
        +TryParseEntero(string, out int) bool$
        +ParseEnteroEstricto(string) int$
        +EsEmailValido(string) bool$
    }

    Sale "1" --> "1" Client : tiene
    Sale "1" *-- "1..*" SaleDetail : contiene
    SaleDetail "1" --> "1" Product : referencia
    Sale --> SaleStatus : estado
```

> Convención de nombres: los DbSets en `ApplicationDbContext` usan nombres en español (`Productos`, `Clientes`, `Ventas`, `DetalleVentas`), mientras que las clases de entidad usan nombres en inglés (`Product`, `Client`, `Sale`, `SaleDetail`).

## Diagrama entidad-relación

Ver imagen en `docs/EDR.png`.

## Notas de arquitectura

- `SaleDetail.Subtotal` es una propiedad calculada (`Cantidad * PrecioUnitario`), no almacenada en base de datos.
- `Sale.Total` es almacenado y se calcula en el page model al crear/editar la venta (`Detalles.Sum(d => d.Subtotal)`).
- `ValidacionCargaMasiva` es una clase utilitaria estática en `Firmeza.Web/Validation/`. Sus métodos son usados tanto por `Pages/CargaMasiva/Index.cshtml.cs` como por los tests unitarios en `tests/Firmeza.UnitTests/CargaMasivaValidacionTests.cs`.
