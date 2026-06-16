using System.Globalization;
using System.Net.Mail;

namespace Firmeza.Web.Validation;

public static class ValidacionCargaMasiva
{
    public static bool PrecioEsValido(decimal precio) => precio > 0;

    public static bool EdadEsValida(int edad) => edad >= 0 && edad <= 120;

    public static bool TryParseEntero(string s, out int value)
    {
        // Handle "100.0" coming from numeric Excel cells converted to string
        if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal d)
            && d == Math.Floor(d))
        {
            value = (int)d;
            return true;
        }
        value = 0;
        return false;
    }

    // Throws FormatException for non-numeric input (wraps int.Parse directly)
    public static int ParseEnteroEstricto(string s) => int.Parse(s);

    public static bool EsEmailValido(string email)
    {
        try { var a = new MailAddress(email); return a.Address == email; }
        catch { return false; }
    }
}
