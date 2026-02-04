using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TF.src.Infra.Processamento.Payloads
{
    public static class PayloadsUtilitario
    {
        private static readonly CultureInfo _inv = CultureInfo.InvariantCulture;
        
        private static readonly Regex _regexFuso = new(@"([+-]\d{2})$", RegexOptions.Compiled);

        private static readonly string[] _formatosData = 
        [
            "dd/MM/yyyy HH:mm:ss",
            "yyyy-MM-dd HH:mm:ss.FFF",
            "yyyy-MM-dd", 
            "yyyy-MM-dd HH:mm:sszzz",
            "yyyy-MM-dd HH:mm:ss.FFFFFFFzzz",
            "MMM d yyyy h:mmtt",
            "MMM d yyyy hh:mmtt",
            "MMM dd yyyy h:mmtt",
            "MMM dd yyyy hh:mmtt"
        ];
        
        public static string Clean(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            var x = s.Replace("[", " ").Replace("]", " ").Trim();
            if(!x.Contains("  ")) return x;
            return string.Join(' ', x.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        public static string JoinId(string? a, string? b, string? c)
        {
            var va = Clean(a);
            var vb = Clean(b);
            var vc = Clean(c);

            if (string.IsNullOrEmpty(va)) return string.IsNullOrEmpty(vb) ? vc : (string.IsNullOrEmpty(vc) ? vb : $"{vb}_{vc}");
            
            var sb = new System.Text.StringBuilder(va.Length + vb.Length + vc.Length + 2);
            sb.Append(va);
            if (!string.IsNullOrEmpty(vb)) sb.Append('_').Append(vb);
            if (!string.IsNullOrEmpty(vc)) sb.Append('_').Append(vc);
            
            return sb.ToString();
        }

        public static string? GetStr(Dictionary<string, JsonElement> ex, string key)
        {
            if (!ex.TryGetValue(key, out var el)) return null;
        
            return el.ValueKind switch
            {
                JsonValueKind.String => el.GetString(),
                JsonValueKind.Number => el.GetRawText(),
                JsonValueKind.True   => "true",
                JsonValueKind.False  => "false",
                _ => el.ToString()
            };
        }

        public static string? GetStrWithoutFL(Dictionary<string, JsonElement> ex, string key)
        {
            if (!ex.TryGetValue(key, out var el)) return null;
            var s = el.ValueKind == JsonValueKind.String ? el.GetString() : el.ToString();
            
            if (string.IsNullOrEmpty(s) || s.Length <= 3) return null;
            return s.Substring(3);
        }

        public static double? GetDouble(Dictionary<string, JsonElement> ex, string key)
        {
            if (!ex.TryGetValue(key, out var el)) return null;

            try
            {
                if (el.ValueKind == JsonValueKind.Number && el.TryGetDouble(out var n))
                    return n;

                if (el.ValueKind == JsonValueKind.String)
                {
                    var s = el.GetString();
                    if (string.IsNullOrWhiteSpace(s)) return null;

                    if (double.TryParse(s, NumberStyles.Float, _inv, out var d))
                        return d;

                    var norm = s.Replace(".", "").Replace(',', '.');
                    if (double.TryParse(norm, NumberStyles.Float, _inv, out d))
                        return d;
                }
            }
            catch { return null; }

            return null;
        }

        public static int? GetInt(Dictionary<string, JsonElement> ex, string key)
        {
            if (!ex.TryGetValue(key, out var el)) return null;

            try
            {
                if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var i))
                    return i;

                if (el.ValueKind == JsonValueKind.String)
                {
                    var s = el.GetString();
                    if (string.IsNullOrWhiteSpace(s)) return null;

                    if (int.TryParse(s, NumberStyles.Any, _inv, out var i2))
                        return i2;
                }
            }
            catch { return null; }

            return null;
        }

        public static string? FmtDate(string? s, string? formato = null, bool salvarUtc = false)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;

            if (s.Contains('T')) s = s.Replace('T', ' ');
            if (s.Contains('Z')) s = s.Replace("Z", "");

            s = _regexFuso.Replace(s, "$1:00");

            try
            {
                if (DateTimeOffset.TryParseExact(
                    s,
                    _formatosData,
                    _inv,
                    DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal,
                    out var d))
                {
                    if (formato is not null) return d.ToString("yyyy-MM-dd");

                    var dtFinal = salvarUtc ? d.UtcDateTime : d.LocalDateTime;
                    return dtFinal.ToString("yyyy-MM-dd HH:mm:ss");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao tentar tratar a data_registro {ex.Message}");
            }                

            return null;
        }

        public static double? Round(this double? value, int digits)
            => value.HasValue ? Math.Round(value.Value, digits, MidpointRounding.AwayFromZero) : null;

        public static double Round(this double value, int digits)
            => Math.Round(value, digits, MidpointRounding.AwayFromZero);
    }
}