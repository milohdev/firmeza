namespace Firmeza.Web.ViewModels;

public class ResultadoCarga
{
    public int ProductosInsertados { get; set; }
    public int ProductosActualizados { get; set; }
    public int ClientesInsertados { get; set; }
    public int ClientesActualizados { get; set; }
    public int VentasCreadas { get; set; }
    public List<ErrorFila> Errores { get; set; } = new();

    public bool TieneResultados =>
        ProductosInsertados + ProductosActualizados +
        ClientesInsertados + ClientesActualizados +
        VentasCreadas + Errores.Count > 0;
}

public record ErrorFila(int Fila, string Descripcion);
