using TF.src.Infra.Modelo;

namespace TF.src.Infra.Processamento.Payloads
{
    public static class ProdutoPayload
    {
        public static bool TryBuild(ApiLinha linha, out Dictionary<string, object?> envelope)
        {
            envelope = new();
            var ex = linha.CamposExtras ?? new();

            var vehicleName = PayloadsUtilitario.Clean(PayloadsUtilitario.GetStr(ex, "vehicle_desc") ?? PayloadsUtilitario.GetStr(ex, "vehicle_name"));
            var equipDate   = PayloadsUtilitario.GetStr(ex, "equip_date");

            var dados = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["id_ident"]           = PayloadsUtilitario.JoinId(vehicleName, null, equipDate),
                ["prefixo"]            = (vehicleName ?? "").Replace("VT-", "", StringComparison.OrdinalIgnoreCase).Trim(),
                ["data_producao"]      = PayloadsUtilitario.FmtDate(equipDate),
                ["quantidade_toras"]   = PayloadsUtilitario.GetInt(ex, "amount"),
                ["volume_toras"]       = PayloadsUtilitario.GetDouble(ex, "volume"),
                ["comprimento_minimo"] = PayloadsUtilitario.GetInt(ex, "min_length"),
                ["comprimento_maximo"] = PayloadsUtilitario.GetInt(ex, "max_length"),
                ["comprimento_medio"]  = PayloadsUtilitario.GetDouble(ex, "avg_length"),
                ["data_registro"]      = PayloadsUtilitario.FmtDate(equipDate, "sim")
            };

            envelope["tabela"] = "TimberFleet_Produtos_producao";
            envelope["dados"]  = dados;
            return true;
        }
    }
}
