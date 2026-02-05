using System.Globalization;
using System.Text.RegularExpressions;
using TF.src.Infra.Modelo;

namespace TF.src.Infra.Processamento.Payloads
{
    public static class LocalizacaoPayload
    {
        public static bool TryBuild(ApiLinha linha, out Dictionary<string, object?> envelope)
        {
            envelope = new();
            var ex = linha.CamposExtras ?? new();

            var vehicleDesc = PayloadsUtilitario.Clean(PayloadsUtilitario.GetStr(ex, "vehicle_desc") ?? PayloadsUtilitario.GetStr(ex, "vehicle_name"));
            var operatorName = PayloadsUtilitario.GetStr(ex, "operator_name");
            var equipDate    = PayloadsUtilitario.GetStr(ex, "equip_date");
            var status       = PayloadsUtilitario.GetStr(ex, "status_desc");

            double? lat = PayloadsUtilitario.GetDouble(ex, "latitude");
            double? lon = PayloadsUtilitario.GetDouble(ex, "longitude");
            
            if ((lat is null || lon is null) && PayloadsUtilitario.GetStr(ex, "latlon") is string latlon && !string.IsNullOrWhiteSpace(latlon))
            {
                var m = Regex.Matches(latlon, @"[-+]?\d+(?:[.,]\d+)?");
                if (m.Count >= 2)
                {
                    static bool parse(string s, out double v)
                    {
                        s = s.Replace(',', '.');
                        return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v);
                    }

                    if (parse(m[0].Value, out var dLat) && parse(m[1].Value, out var dLon))
                    { lat = dLat; lon = dLon; }
                }
            }


            var dados = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["id_ident"]         = PayloadsUtilitario.JoinId(vehicleDesc, operatorName, equipDate),
                ["nome_operador"]    = operatorName,
                ["prefixo"]          = (vehicleDesc ?? "").Replace("VT-", "", StringComparison.OrdinalIgnoreCase).Trim(),
                ["data_localizacao"] = PayloadsUtilitario.FmtDate(equipDate),
                ["latitude"]         = lat,
                ["longitude"]        = lon,
                ["operacao_status"]  = status,
                ["data_registro"]    = PayloadsUtilitario.FmtDate(equipDate, "sim")
            };

            envelope["tabela"] = "TimberFleet_Localizacao";
            envelope["dados"]  = dados;
            return true;
        }
    }
}
