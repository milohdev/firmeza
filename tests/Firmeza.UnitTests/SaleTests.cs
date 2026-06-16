using Firmeza.Web.Models;

namespace Firmeza.UnitTests;

public class SaleTests
{
    // ── Total = suma de subtotales ────────────────────────────────────────────

    [Fact]
    public void Total_DeberiaSerSumaCorrectaDeSubtotalesDeDetalles()
    {
        var venta = new Sale();
        venta.Detalles.Add(new SaleDetail { Cantidad = 2, PrecioUnitario = 1000m });
        venta.Detalles.Add(new SaleDetail { Cantidad = 3, PrecioUnitario = 500m });

        // 2*1000 + 3*500 = 2000 + 1500 = 3500
        var totalCalculado = venta.Detalles.Sum(d => d.Subtotal);

        Assert.Equal(3500m, totalCalculado);
    }

    [Fact]
    public void Total_SinDetalles_DeberiaSerCero()
    {
        var venta = new Sale();

        var totalCalculado = venta.Detalles.Sum(d => d.Subtotal);

        Assert.Equal(0m, totalCalculado);
    }

    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(5, 20000, 100000)]
    [InlineData(10, 500, 5000)]
    public void Total_ConDetalle_CalculaSubtotalCorrectamente(int cantidad, double precio, double subtotalEsperado)
    {
        var detalle = new SaleDetail
        {
            Cantidad = cantidad,
            PrecioUnitario = (decimal)precio
        };

        Assert.Equal((decimal)subtotalEsperado, detalle.Subtotal);
    }

    // ── IVA 19% ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(10000, 1900)]
    [InlineData(5000, 950)]
    [InlineData(1000, 190)]
    [InlineData(100, 19)]
    public void Iva_DeberiaCalcularseCorrectamente(decimal total, decimal ivaEsperado)
    {
        var iva = Math.Round(total * 0.19m, 2);

        Assert.Equal(ivaEsperado, iva);
    }

    [Fact]
    public void Iva_TotalConIva_DeberiaSerSubtotalMasIva()
    {
        const decimal subtotal = 10000m;
        var iva = Math.Round(subtotal * 0.19m, 2);
        var totalConIva = subtotal + iva;

        Assert.Equal(11900m, totalConIva);
    }
}
