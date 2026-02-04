using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TF.src.Infra.Processamento
{
    internal static class Utilidades
    {
        private static readonly CultureInfo _inv = CultureInfo.InvariantCulture;
        private static readonly string[] _formatos =
            [
                "yyyy-MM-dd HH:mm:ss",
                "yyyy-MM-dd",
                "dd/MM/yyyy HH:mm:ss",
                "dd/MM/yyyy"
            ];

        public static bool TentarPegarData(string? str, out DateTimeOffset data)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                data = default;
                return false;
            }

            if (DateTimeOffset.TryParse(str, _inv, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out data)) return true;

            if (DateTime.TryParseExact(str, _formatos, _inv, DateTimeStyles.AssumeLocal, out var dt))
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

            if (extra.TryGetValue(chave, out var elemento) && elemento.ValueKind == JsonValueKind.String)
            {
                value = elemento.GetString() ?? string.Empty;
                return true;
            }
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
                    if (string.IsNullOrEmpty(str)) return false;

                    if (str.Contains(',')) str = str.Replace(',', '.');

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
        {
            if (string.IsNullOrEmpty(str)) return string.Empty;
            if (!str.Contains('[') && !str.Contains(']')) return str;
            return str.Replace("[", " ").Replace("]", " ").Trim();
        }

        public static bool TentarSepararDoubles(string input, out double a, out double b)
        {
            a = b = 0d;
            input ??= "";

            var m = Regex.Matches(input, @"[-+]?\d+(?:[.,]\d+)?");
            if (m.Count < 2) return false;

            static bool parse(string s, out double v)
            {
                s = s.Replace(',', '.');
                return double.TryParse(s, NumberStyles.Float, _inv, out v);
            }

            return parse(m[0].Value, out a) && parse(m[1].Value, out b);
        }
    }
}