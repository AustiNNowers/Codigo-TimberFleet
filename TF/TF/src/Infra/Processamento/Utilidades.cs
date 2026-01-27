using System.Globalization;
using System.Text.Json;

namespace TF.src.Infra.Processamento
{
    internal static class Utilidades
    {
        private static readonly CultureInfo _inv = CultureInfo.InvariantCulture;

        public static bool TentarPegarData(string? str, out DateTimeOffset data)
        {
            if (!string.IsNullOrWhiteSpace(str) &&
                DateTimeOffset.TryParse(str, _inv, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out data)) return true;

            string[] formatos =
            [
                "yyyy-MM-dd HH:mm:ss",
                "yyyy-MM-dd",
                "dd/MM/yyyy HH:mm:ss",
                "dd/MM/yyyy"
            ];

            if (!string.IsNullOrWhiteSpace(str) &&
                DateTime.TryParseExact(str, formatos, _inv, DateTimeStyles.AssumeLocal, out var dt))
            {
                data = new DateTimeOffset(dt).ToUniversalTime();
                return true;
            }

            data = default;
            return false;
        }

        public static bool TentarPegarString(Dictionary<string, JsonElement>? extra, string chave, out string value)
        {
            value = string.Empty;
            if (extra is null) return false;
            if (!extra.TryGetValue(chave, out var elemento)) return false;
            if (elemento.ValueKind == JsonValueKind.String) { value = elemento.GetString() ?? string.Empty; return true; }
            return false;
        }
        public static bool TentarPegarDouble(Dictionary<string, JsonElement>? extra, string chave, out double value)
        {
            value = 0d;
            if (extra is null) return false;
            if (!extra.TryGetValue(chave, out var elemento)) return false;

            switch (elemento.ValueKind)
            {
                case JsonValueKind.Number:
                    return elemento.TryGetDouble(out value);
                case JsonValueKind.String:
                    var str = elemento.GetString() ?? "";
                    str = str.Replace(',', '.');
                    return double.TryParse(str, NumberStyles.Float | NumberStyles.AllowThousands, _inv, out value);
                default:
                    return false;
            }
        }

        public static void Set(Dictionary<string, JsonElement> extra, string chave, string? value)
            => extra[chave] = JsonSerializer.SerializeToElement(value);
        public static void Set(Dictionary<string, JsonElement> extra, string chave, double value)
            => extra[chave] = JsonSerializer.SerializeToElement(value);
        public static void Set(Dictionary<string, JsonElement> extra, string chave, float value)
            => extra[chave] = JsonSerializer.SerializeToElement(value);
        public static void Set(Dictionary<string, JsonElement> extra, string chave, long value)
            => extra[chave] = JsonSerializer.SerializeToElement(value);
        
        public static void Remover(Dictionary<string, JsonElement> extra, string chave)
        {
            if (extra is null) return;
            extra.Remove(chave);
        }

        public static string LimparColchetes(string str)
            => str.Replace("[", " ").Replace("]", " ").Trim();

        public static bool TentarSepararDoubles(string input, out double a, out double b)
        {
            a = b = 0d;
            var partes = (input ?? "").Trim().Split(new[] { ' ', ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length < 2) return false;
            return double.TryParse(partes[0].Replace(',', '.'), NumberStyles.Float, _inv, out a) &&
                   double.TryParse(partes[1].Replace(',', '.'), NumberStyles.Float, _inv, out b);
        }
    }
}