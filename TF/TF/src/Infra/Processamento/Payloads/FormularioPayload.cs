namespace TF.src.Infra.Processamento.Payloads
{
    public static class FormularioPayload
    {
        public static bool TryBuild(ApiLinha linha, out Dictionary<string, object?> envelope)
        {
            envelope = new();
            var ex = linha.CamposExtras ?? new();

            var vehicleDesc  = Clean(GetStr(ex, "vehicle_desc") ?? GetStr(ex, "vehicle_name"));
            var operatorName = GetStr(ex, "operator_name");
            var operatorCode = GetStr(ex, "operator_code");
            var equipDate    = GetStr(ex, "equip_date");
            var status       = GetStr(ex, "status_desc");
            var formTitle    = GetStr(ex, "form_title");

            var dados = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["id_ident"]          = JoinId(vehicleDesc, operatorName, equipDate),
                ["prefixo"]           = (vehicleDesc ?? "").Replace("VT-", "", StringComparison.OrdinalIgnoreCase).Trim(),
                ["data_formulario"]   = FmtDate(equipDate),
                ["nome_operador"]     = operatorName,
                ["codigo_operador"]   = operatorCode,
                ["operacao_status"]   = status,
                ["titulo_formulario"] = formTitle,

                ["matricula_operador"]             = GetStr(ex, "matricula_operador"),
                ["matricula_comboista"]            = GetStr(ex, "matricula_comboista"),
                ["matricula_mecanico"]             = GetStr(ex, "matricula_mecanico"),
                ["tipo_falha"]                     = GetStr(ex, "tipo_falha"),
                ["atividade_anterior_apropriacao"] = GetStr(ex, "atividade_anterior_apropriacao"),
                ["avaliacao"]                      = GetStr(ex, "tipo_avaliacao") ?? GetStr(ex, "avaliacao"),
                ["equipamento_disponivel"]         = GetStr(ex, "equipamento_disponivel"),
                ["motivo_indisponibilidade"]       = GetStr(ex, "motivo_indisponibilidade"),
                ["horimetro_motor"]                = GetDouble(ex, "horimetro_motor"),
                ["tipo_oleo"]                      = GetStr(ex, "tipo_oleo"),
                ["quantidade_abastecida"]          = GetDouble(ex, "quantidade_abastecida"),
                ["tipo_produto"]                   = GetStr(ex, "tipo_produto"),
                ["volume_carga_caminhao"]          = GetStr(ex, "volume_carga_caminhao"),
                ["frota_caminhao"]                 = GetStr(ex, "frota_caminhao"),
                ["balanca"]                        = GetStr(ex, "balanca"),
                ["box_descarga"]                   = GetStr(ex, "box_descarga"),
                ["motivo"]                         = GetStr(ex, "motivo"),
                ["status_inicio_abastecimento"]    = GetStr(ex, "status_inicio_abastecimento"),
                ["tipo_operacao"]                  = GetStr(ex, "tipo_operacao"),
                ["talhao_operacao"]                = GetStr(ex, "talhao_operacao"),
                ["quantidade_arvores_cortadas"]    = GetInt(ex, "quantidade_arvores_cortadas"),
                ["quantidade_mudas"]               = GetInt(ex, "quantidade_mudas"),
                ["informe_producao"]               = GetDouble(ex, "informe_producao"),
                ["volume_caixa_carga"]             = GetStr(ex, "volume_caixa_carga"),
                ["tipo_servico"]                   = GetStr(ex, "tipo_servico"),
                ["quantidade_toco"]                = GetInt(ex, "quantidade_toco"),
                ["hectares"]                       = GetDouble(ex, "hectares"),
                ["parada"]                         = GetStr(ex, "parada"),

                ["data_registro"] = FmtDate(equipDate, "sim")
            };

            envelope["tabela"] = "TimberFleet_Formulario";
            envelope["dados"]  = dados;
            return true;
        }
    }
}