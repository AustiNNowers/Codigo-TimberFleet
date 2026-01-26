using System.Globalization;
using System.Text.Json;
using TF.src.Infra.Modelo;
using static TF.src.Infra.Processamento.Payloads.PayloadsUtilitario;

namespace TF.src.Infra.Processamento.Payloads
{
    public static class ProdutoPayload
    {
        public static bool TryBuild(ApiLinha linha, out Dictionary<string, object?> envelope)
        {
            envelope = new();
            var ex = linha.CamposExtras ?? new();

            var vehicleName = Clean(GetStr(ex, "vehicle_desc") ?? GetStr(ex, "vehicle_name"));
            var equipDate   = GetStr(ex, "equip_date");

            var dados = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["id_ident"]           = JoinId(vehicleName, null, equipDate),
                ["prefixo"]            = (vehicleName ?? "").Replace("VT-", "", StringComparison.OrdinalIgnoreCase).Trim(),
                ["data_producao"]      = FmtDate(equipDate),
                ["quantidade_toras"]   = GetInt(ex, "amount"),
                ["volume_toras"]       = GetDouble(ex, "volume"),
                ["comprimento_minimo"] = GetInt(ex, "min_length"),
                ["comprimento_maximo"] = GetInt(ex, "max_length"),
                ["comprimento_medio"]  = GetDouble(ex, "avg_length"),
                ["data_registro"]      = FmtDate(equipDate, "sim")
            };

            envelope["tabela"] = "TimberFleet_Produtos_producao";
            envelope["dados"]  = dados;
            return true;
        }
    }
}
