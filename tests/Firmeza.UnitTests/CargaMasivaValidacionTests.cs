using Firmeza.Web.Validation;

namespace Firmeza.UnitTests;

public class CargaMasivaValidacionTests
{
    // ── Precio ───────────────────────────────────────────────────────────────

    [Fact]
    public void Precio_Negativo_EsInvalido()
    {
        Assert.False(ValidacionCargaMasiva.PrecioEsValido(-1m));
    }

    [Fact]
    public void Precio_Cero_EsInvalido()
    {
        Assert.False(ValidacionCargaMasiva.PrecioEsValido(0m));
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(1000)]
    [InlineData(99999.99)]
    public void Precio_Positivo_EsValido(double precio)
    {
        Assert.True(ValidacionCargaMasiva.PrecioEsValido((decimal)precio));
    }

    // ── Edad ─────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(-1)]
    [InlineData(121)]
    [InlineData(-100)]
    [InlineData(200)]
    public void Edad_FueraDeRango_EsInvalida(int edad)
    {
        Assert.False(ValidacionCargaMasiva.EdadEsValida(edad));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(60)]
    [InlineData(119)]
    [InlineData(120)]
    public void Edad_Valida_Pasa(int edad)
    {
        Assert.True(ValidacionCargaMasiva.EdadEsValida(edad));
    }

    // ── ParseEnteroEstricto ───────────────────────────────────────────────────

    [Theory]
    [InlineData("abc")]
    [InlineData("doce")]
    [InlineData("")]
    [InlineData("12.5a")]
    public void ParseEnteroEstricto_ConStringNoNumerico_LanzaFormatException(string entrada)
    {
        Assert.Throws<FormatException>(() => ValidacionCargaMasiva.ParseEnteroEstricto(entrada));
    }

    // ── TryParseEntero ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("5", 5)]
    [InlineData("100.0", 100)]
    [InlineData("0", 0)]
    public void TryParseEntero_ConValorValido_RetornaTrueYValor(string entrada, int esperado)
    {
        var ok = ValidacionCargaMasiva.TryParseEntero(entrada, out int resultado);
        Assert.True(ok);
        Assert.Equal(esperado, resultado);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12.5")]
    [InlineData("")]
    public void TryParseEntero_ConValorInvalido_RetornaFalse(string entrada)
    {
        Assert.False(ValidacionCargaMasiva.TryParseEntero(entrada, out _));
    }
}
