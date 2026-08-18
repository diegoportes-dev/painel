using System.Text.RegularExpressions;
using System.Text;

public static class SlugHelper
{
    public static string GerarSlug(string texto)
    {
        string textoNormalizado = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (char c in textoNormalizado)
        {
            var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        string semAcentos = sb.ToString().ToLower();
        // Remove tudo que não for letra, número ou espaço
        string apenasLetrasNumeros = Regex.Replace(semAcentos, @"[^a-z0-9\s-]", "");
        // Transforma espaços múltiplos em um único hífen
        string semEspaco = Regex.Replace(apenasLetrasNumeros, @"\s+", "").Trim();
        
        return semEspaco;
    }
}
