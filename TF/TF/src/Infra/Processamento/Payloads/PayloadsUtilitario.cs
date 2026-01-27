using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TF.src.Infra.Processamento.Payloads
{
    public static class PayloadsUtilitario
    {
        public static string Clean(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            var x = s.Replace("[", " ").Replace("]", " ").Trim();
            return string.Join(' ', x.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        public static string JoinId(string? a, string? b, string? c)
            => string.Join('_', new[] { Clean(a), Clean(b), Clean(c) }
                .Where(z => !string.IsNullOrEmpty(z)));

        public static string? GetStr(Dictionary<string, JsonElement> ex, string key)
        {
            if (!ex.TryGetValue(key, out var el)) return null;
            try
            {
                return el.ValueKind switch
                {
                    JsonValueKind.String => el.GetString(),
                    JsonValueKind.Number => el.GetRawText(),
                    JsonValueKind.True   => "true",
                    JsonValueKind.False  => "false",
                    _ => el.ToString()
                };
            }
            catch { return null; }
        }

        public static string? GetStrWithoutFL(Dictionary<string, JsonElement> ex, string key)
        {
            if (!ex.TryGetValue(key, out var el)) return null;
            try { return el.GetString().Remove(0, 3); }
            catch { return null; }
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

                    if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                        return d;

                    var norm = s.Replace(".", "").Replace(',', '.');
                    if (double.TryParse(norm, NumberStyles.Float, CultureInfo.InvariantCulture, out d))
                        return d;
                }
            }
            catch {  }

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

                    if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i2))
                        return i2;
                }
            }
            catch {  }

            return null;
        }

        public static string? FmtDate(string? s, string? formato = null, bool salvarUtc = false)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;

            s = s.Trim().Replace('T', ' ').Replace("Z", "");
            s = Regex.Replace(s, @"([+-]\d{2})$", "$1:00");

            string[] fmts = [
                "dd/MM/yyyy HH:mm:ss",
                "yyyy-MM-dd HH:mm:ss.FFF",
                "yyyy-MM-dd", 

                "yyyy-MM-dd HH:mm:sszzz",
                "yyyy-MM-dd HH:mm:ss.FFFFFFFzzz",

                "dd/MM/yyyy HH:mm:ss",
                "dd/MM/yyyy",

                "MMM d yyyy h:mmtt",
                "MMM d yyyy hh:mmtt",
                "MMM  d yyyy  h:mmtt",
                "MMM  d yyyy  hh:mmtt",
                "MMM dd yyyy h:mmtt",
                "MMM dd yyyy hh:mmtt",
                "MMM  dd yyyy  h:mmtt",
                "MMM  dd yyyy  hh:mmtt"
            ];

            if (formato is not null)
            {
                try
                {
                    if (DateTimeOffset.TryParseExact(
                        s,
                        fmts,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeLocal,
                        out var d))
                    {
                        return d.ToString("yyyy-MM-dd");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao tentar tratar a data_registro {ex.Message}");
                }                
            }

            if (DateTimeOffset.TryParseExact(
                    s,
                    fmts,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces,
                    out var dto))
            {
                var dtFinal = salvarUtc ? dto.UtcDateTime : dto.LocalDateTime;
                return dtFinal.ToString("yyyy-dd-MM HH:mm:ss");
            }

            if (DateTime.TryParseExact(
                    s,
                    fmts,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces,
                    out var dt))
            {
                return dt.ToString("yyyy-dd-MM HH:mm:ss");
            }

            return null;
        }

        public static string NowUtc() => DateTime.UtcNow.ToString("dd/MM/yyyy");

        public static double? Round(this double? value, int digits)
            => value is null ? null : Math.Round(value.Value, digits, MidpointRounding.AwayFromZero);

        public static double Round(this double value, int digits)
            => Math.Round(value, digits, MidpointRounding.AwayFromZero);
    }
}