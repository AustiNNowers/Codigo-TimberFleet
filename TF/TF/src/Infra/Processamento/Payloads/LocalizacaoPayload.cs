using System.Globalization;

namespace TF.src.Infra.Processamento.Payloads
{
    public static class LocalizacaoPayload
    {
        public static bool TryBuild(ApiLinha linha, out Dictionary<string, object?> envelope)
        {
            envelope = new();
            var ex = linha.CamposExtras ?? new();

            var vehicleDesc = Clean(GetStr(ex, "vehicle_desc") ?? GetStr(ex, "vehicle_name"));
            var operatorName = GetStr(ex, "operator_name");
            var equipDate    = GetStr(ex, "equip_date");
            var status       = GetStr(ex, "status_desc");

            double? lat = GetDouble(ex, "latitude");
            double? lon = GetDouble(ex, "longitude");

            if ((lat is null || lon is null) && GetStr(ex, "latlon") is string latlon && !string.IsNullOrWhiteSpace(latlon))
            {
                var p = latlon.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (p.Length == 2 &&
                    double.TryParse(p[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var dLat) &&
                    double.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var dLon))
                { lat = dLat; lon = dLon; }
            }

            var dados = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["id_ident"]         = JoinId(vehicleDesc, operatorName, equipDate),
                ["nome_operador"]    = operatorName,
                ["prefixo"]          = (vehicleDesc ?? "").Replace("VT-", "", StringComparison.OrdinalIgnoreCase).Trim(),
                ["data_localizacao"] = FmtDate(equipDate),
                ["latitude"]         = lat,
                ["longitude"]        = lon,
                ["operacao_status"]  = status,
                ["data_registro"]    = FmtDate(equipDate, "sim")
            };

            envelope["tabela"] = "TimberFleet_Localizacao";
            envelope["dados"]  = dados;
            return true;
        }
    }
}
