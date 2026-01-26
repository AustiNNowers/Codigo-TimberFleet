using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using TF.src.Infra.Modelo;

namespace TF.src.Infra.Processamento
{
    public class LimparDados(OpcoesLimpeza? opcoes = null) : ILimparDados
    {
        private readonly CultureInfo _invariante = CultureInfo.InvariantCulture;
        private readonly OpcoesLimpeza _opcoes = opcoes ?? new OpcoesLimpeza();
        public ApiLinha Limpar(ApiLinha linha)
        {
            if (_opcoes.NormalizarUpdateAtIso && !string.IsNullOrWhiteSpace(linha.UpdatedAtIso))
            {
                if (DateTimeOffset.TryParse(linha.UpdatedAtIso, _invariante,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
                {
                    linha.UpdatedAtIso = dt.ToUniversalTime().ToString("O", _invariante);
                }
            }

            if (linha.CamposExtras is not null && linha.CamposExtras.Count > 0)
                return linha;

            bool haveraMudanca = false;
            foreach (var kv in linha.CamposExtras)
            {
                if (kv.Value.ValueKind == JsonValueKind.String)
                {
                    var s = kv.Value.GetString() ?? string.Empty;
                    var t = _opcoes.RemoverEspaços ? s.Trim() : s;

                    if (_opcoes.RemoverStringsVazias && t.Length == 0) { haveraMudanca = true; break; }
                    if (_opcoes.ParseNumerosString && ParseNumeros(t, out _)) { haveraMudanca = true; break; }
                    if (_opcoes.ParseBooleanosString && ParseBooleano(t, out _)) { haveraMudanca = true; break; }
                    if (_opcoes.ParseDatasString && ParseDate(t, out _)) { haveraMudanca = true; break; }
                    if (!ReferenceEquals(t, s) && t != s) { haveraMudanca = true; break; }
                }
            }

            if (!haveraMudanca)
                return linha;

            var novo = new Dictionary<string, JsonElement>(linha.CamposExtras.Count, StringComparer.OrdinalIgnoreCase);

            foreach (var (key, value) in linha.CamposExtras)
            {
                if (value.ValueKind == JsonValueKind.String)
                {
                    var s = value.GetString() ?? string.Empty;
                    var t = _opcoes.RemoverEspaços ? s.Trim() : s;

                    if (_opcoes.RemoverStringsVazias && t.Length == 0)
                        continue;

                    if (_opcoes.ParseNumerosString && ParseNumeros(t, out var num))
                    {
                        novo[key] = JsonSerializer.SerializeToElement(num);
                        continue;
                    }
                    if (_opcoes.ParseBooleanosString && ParseBooleano(t, out var b))
                    {
                        novo[key] = JsonSerializer.SerializeToElement(b);
                        continue;
                    }
                    if (_opcoes.ParseDatasString && ParseDate(t, out var dt))
                    {
                        novo[key] = JsonSerializer.SerializeToElement(dt.ToUniversalTime().ToString("O", _invariante));
                        continue;
                    }

                    if (!ReferenceEquals(t, s) && t != s)
                    {
                        novo[key] = JsonSerializer.SerializeToElement(t);
                        continue;
                    }

                    novo[key] = value;
                }
                else
                {
                    novo[key] = value;
                }
            }

            linha.CamposExtras = novo;
            return linha;
        }

        public IEnumerable<ApiLinha> LimparVariasLinhas(IEnumerable<ApiLinha> linhas)
        {
            foreach (var linha in linhas) yield return Limpar(linha);
        }

        private static bool ParseNumeros(string str, out double value)
        {
            if (double.TryParse(str, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value))
                return true;

            var ptbr = new CultureInfo("pt-BR");
            if (double.TryParse(str, NumberStyles.Float | NumberStyles.AllowThousands, ptbr, out value))
                return true;

            var normalized = str.Replace(".", string.Empty).Replace(',', '.');
            if (double.TryParse(normalized, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value))
                return true;

            value = default;
            return false;
        }

        private static bool ParseBooleano(string str, out bool value)
        {
            if (bool.TryParse(str, out value)) return true;
            var valor = str.Trim().ToLowerInvariant();
            if (valor is "true" or "t" or "yes" or "y" or "1") { value = true; return true; }
            if (valor is "false" or "f" or "no" or "n" or "0") { value = false; return true; }
            value = default; return false;
        }

        private bool ParseDate(string str, out DateTimeOffset isoUtc)
        {
            if (!string.IsNullOrWhiteSpace(str) &&
                DateTimeOffset.TryParse(str, _invariante, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out isoUtc))
                return true;

            string[] formatos =
            {
                "dd/MM/yyyy HH:mm:ss",
                "dd/MM/yyyy HH:mm",
                "dd/MM/yyyy",
                "yyyy-MM-dd HH:mm:ss",
                "yyyy-MM-dd HH:mm",
                "yyyy-MM-dd"
            };

            if (!string.IsNullOrWhiteSpace(str) &&
                DateTime.TryParseExact(str, formatos, _invariante, DateTimeStyles.AssumeLocal, out var dt))
            {
                isoUtc = new DateTimeOffset(dt).ToUniversalTime();
                return true;
            }

            isoUtc = default;
            return false;
        }
    }
}