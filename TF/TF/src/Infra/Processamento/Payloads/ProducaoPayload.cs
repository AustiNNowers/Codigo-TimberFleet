namespace TF.src.Infra.Processamento.Payloads
{
    public static class ProducaoPayload
    {
        public static bool TryBuild(ApiLinha linha, out Dictionary<string, object?> envelope)
        {
            envelope = new();
            var ex = linha.CamposExtras ?? new();

            var vehicleName  = Clean(GetStr(ex, "vehicle_desc") ?? GetStr(ex, "vehicle_name"));
            var operatorName = GetStr(ex, "operator_name");
            var equipDate    = GetStr(ex, "equip_date");

            var dados = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["id_ident"]         = JoinId(vehicleName, operatorName, equipDate),
                ["tipo_arquivo"]     = GetStr(ex, "file_type"),
                ["prefixo"]          = (vehicleName ?? "").Replace("VT-", "", StringComparison.OrdinalIgnoreCase).Trim(),
                ["nome_operador"]    = operatorName,
                ["data_producao"]    = FmtDate(equipDate),
                ["contagem_arvores"] = GetInt(ex, "tree_amount"),
                ["volume_total"]     = GetDouble(ex, "volume_total"),
                ["data_registro"]    = FmtDate(equipDate, "sim")
            };

            envelope["tabela"] = "TimberFleet_Producao";
            envelope["dados"]  = dados;
            return true;
        }
    }
}
