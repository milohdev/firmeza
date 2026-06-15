using Firmeza.Web.Models;
using Xunit;

namespace Firmeza.UnitTests;

public class SaleDetailTests
{
    [Fact]
    public void Subtotal_DeberiaCalcularseCorrectamente()
    {
        // Arrange
        var detalle = new SaleDetail
        {
            Cantidad = 3,
            PrecioUnitario = 25000m
        };

        // Act
        var resultado = detalle.Subtotal;

        // Assert
        Assert.Equal(75000m, resultado);
    }

    [Theory]
    [InlineData(1, 1000, 1000)]
    [InlineData(5, 2000, 10000)]
    [InlineData(0, 5000, 0)]
    public void Subtotal_ConDistintosValores_CalculaCorrectamente(int cantidad, decimal precio, decimal esperado)
    {
        var detalle = new SaleDetail
        {
            Cantidad = cantidad,
            PrecioUnitario = precio
        };

        Assert.Equal(esperado, detalle.Subtotal);
    }
}