using TF.src.Infra.Modelo;

namespace TF.src.Infra.Processamento.Payloads
{
    public static class ProducaoPayload
    {
        public static bool TryBuild(ApiLinha linha, out Dictionary<string, object?> envelope)
        {
            envelope = new();
            var ex = linha.CamposExtras ?? new();

            var vehicleName  = PayloadsUtilitario.Clean(PayloadsUtilitario.GetStr(ex, "vehicle_desc") ?? PayloadsUtilitario.GetStr(ex, "vehicle_name"));
            var operatorName = PayloadsUtilitario.GetStr(ex, "operator_name");
            var equipDate    = PayloadsUtilitario.GetStr(ex, "equip_date");

            var dados = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["id_ident"]         = PayloadsUtilitario.JoinId(vehicleName, operatorName, equipDate),
                ["tipo_arquivo"]     = PayloadsUtilitario.GetStr(ex, "file_type"),
                ["prefixo"]          = (vehicleName ?? "").Replace("VT-", "", StringComparison.OrdinalIgnoreCase).Trim(),
                ["nome_operador"]    = operatorName,
                ["data_producao"]    = PayloadsUtilitario.FmtDate(equipDate),
                ["contagem_arvores"] = PayloadsUtilitario.GetInt(ex, "tree_amount"),
                ["volume_total"]     = PayloadsUtilitario.GetDouble(ex, "volume_total"),
                ["data_registro"]    = PayloadsUtilitario.FmtDate(equipDate, "sim")
            };

            envelope["tabela"] = "TimberFleet_Producao";
            envelope["dados"]  = dados;
            return true;
        }
    }
}
