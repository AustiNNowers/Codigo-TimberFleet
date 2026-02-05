using System.Globalization;
using System.Text.Json;
using TF.src.Infra.Modelo;

namespace TF.src.Infra.Processamento
{
    public class LimparDados(OpcoesLimpeza? opcoes = null) : ILimparDados
    {
        private readonly CultureInfo _invariante = CultureInfo.InvariantCulture;
        private static readonly string[] _formatosData =
        [
            "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy HH:mm", "dd/MM/yyyy",
            "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm", "yyyy-MM-dd"
        ];

        private readonly OpcoesLimpeza _opcoes = opcoes ?? new OpcoesLimpeza();

        public ApiLinha Limpar(ApiLinha linha)
        {
            if (_opcoes.NormalizarUpdateAtIso && 
                !string.IsNullOrWhiteSpace(linha.UpdatedAtIso) &&
                DateTimeOffset.TryParse(linha.UpdatedAtIso, _invariante, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
            {
                linha.UpdatedAtIso = dt.ToUniversalTime().ToString("O", _invariante);
            }

            if (linha.CamposExtras is null || linha.CamposExtras.Count == 0)
                return linha;

            Dictionary<string, JsonElement>? novosCampos = null;
            int indice = 0;

            foreach (var kv in linha.CamposExtras)
            {
                if (novosCampos is not null)
                {
                    novosCampos[kv.Key] = ProcessarValor(kv.Value);
                    continue;
                }

                var valorProcessado = ProcessarValor(kv.Value);

                bool mudou = !ValorIgual(kv.Value, valorProcessado);

                if (mudou)
                {
                    novosCampos = new Dictionary<string, JsonElement>(linha.CamposExtras.Count, StringComparer.OrdinalIgnoreCase);
                    
                    foreach (var anterior in linha.CamposExtras.Take(indice))
                    {
                        novosCampos[anterior.Key] = anterior.Value;
                    }

                    novosCampos[kv.Key] = valorProcessado;
                }

                indice++;
            }

            if (novosCampos is not null)
            {
                linha.CamposExtras = novosCampos;
            }

            return linha;
        }

        private JsonElement ProcessarValor(JsonElement original)
        {
            if (original.ValueKind != JsonValueKind.String) return original;

            var s = original.GetString() ?? string.Empty;
            var t = _opcoes.RemoverEspaços ? s.Trim() : s;

            if (_opcoes.RemoverStringsVazias && t.Length == 0) 
                return default;

            if (_opcoes.ParseNumerosString && ParseNumeros(t, out var num))
                return JsonSerializer.SerializeToElement(num);
            
            if (_opcoes.ParseBooleanosString && ParseBooleano(t, out var b))
                return JsonSerializer.SerializeToElement(b);
            
            if (_opcoes.ParseDatasString && ParseDate(t, out var dt))
                return JsonSerializer.SerializeToElement(dt.ToUniversalTime().ToString("O", _invariante));

            if (!ReferenceEquals(t, s) && t != s)
                return JsonSerializer.SerializeToElement(t);

            return original;
        }

        private static bool ValorIgual(JsonElement a, JsonElement b)
        {
            if (a.ValueKind != b.ValueKind) return false;
            
            if (a.ValueKind == JsonValueKind.String)
                return string.Equals(a.GetString(), b.GetString());
                
            return true; 
        }

        public IEnumerable<ApiLinha> LimparVariasLinhas(IEnumerable<ApiLinha> linhas)
        {
            foreach (var linha in linhas) yield return Limpar(linha);
        }

        private static bool ParseNumeros(string str, out double value)
        {
            if (double.TryParse(str, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value))
                return true;

            if (str.Contains('.') || str.Contains(','))
            {
                var normalized = str.Replace(".", string.Empty).Replace(',', '.');
                if (double.TryParse(normalized, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value))
                    return true;
            }

            value = default;
            return false;
        }

        private static bool ParseBooleano(string str, out bool value)
        {
            if (bool.TryParse(str, out value)) return true;

            var valor = str.Trim().ToLowerInvariant();
            
            switch (valor)
            {
                case "true": case "t": case "yes": case "y": case "1":
                    value = true; return true;
                case "false": case "f": case "no": case "n": case "0":
                    value = false; return true;
            }
            
            value = default; return false;
        }

        private bool ParseDate(string str, out DateTimeOffset isoUtc)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                isoUtc = default;
                return false;
            }

            if (DateTimeOffset.TryParse(str, _invariante, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out isoUtc))
                return true;

            if (DateTime.TryParseExact(str, _formatosData, _invariante, DateTimeStyles.AssumeLocal, out var dt))
            {
                isoUtc = new DateTimeOffset(dt).ToUniversalTime();
                return true;
            }

            isoUtc = default;
            return false;
        }
    }
}