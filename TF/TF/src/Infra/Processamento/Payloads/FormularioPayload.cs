using TF.src.Infra.Modelo;

namespace TF.src.Infra.Processamento.Payloads
{
    public static class FormularioPayload
    {
        public static bool TryBuild(ApiLinha linha, out Dictionary<string, object?> envelope)
        {
            envelope = new();
            var ex = linha.CamposExtras ?? new();

            var vehicleDesc  = PayloadsUtilitario.Clean(PayloadsUtilitario.GetStr(ex, "vehicle_desc") ?? PayloadsUtilitario.GetStr(ex, "vehicle_name"));
            var operatorName = PayloadsUtilitario.GetStr(ex, "operator_name");
            var operatorCode = PayloadsUtilitario.GetStr(ex, "operator_code");
            var equipDate    = PayloadsUtilitario.GetStr(ex, "equip_date");
            var status       = PayloadsUtilitario.GetStr(ex, "status_desc");
            var formTitle    = PayloadsUtilitario.GetStr(ex, "form_title");

            var dados = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["id_ident"]          = PayloadsUtilitario.JoinId(vehicleDesc, operatorName, equipDate),
                ["prefixo"]           = (vehicleDesc ?? "").Replace("VT-", "", StringComparison.OrdinalIgnoreCase).Trim(),
                ["data_formulario"]   = PayloadsUtilitario.FmtDate(equipDate),
                ["nome_operador"]     = operatorName,
                ["codigo_operador"]   = operatorCode,
                ["operacao_status"]   = status,
                ["titulo_formulario"] = formTitle,

                ["matricula_operador"]             = PayloadsUtilitario.GetStr(ex, "matricula_operador"),
                ["matricula_comboista"]            = PayloadsUtilitario.GetStr(ex, "matricula_comboista"),
                ["matricula_mecanico"]             = PayloadsUtilitario.GetStr(ex, "matricula_mecanico"),
                ["tipo_falha"]                     = PayloadsUtilitario.GetStr(ex, "tipo_falha"),
                ["atividade_anterior_apropriacao"] = PayloadsUtilitario.GetStr(ex, "atividade_anterior_apropriacao"),
                ["avaliacao"]                      = PayloadsUtilitario.GetStr(ex, "tipo_avaliacao") ?? PayloadsUtilitario.GetStr(ex, "avaliacao"),
                ["equipamento_disponivel"]         = PayloadsUtilitario.GetStr(ex, "equipamento_disponivel"),
                ["motivo_indisponibilidade"]       = PayloadsUtilitario.GetStr(ex, "motivo_indisponibilidade"),
                ["horimetro_motor"]                = PayloadsUtilitario.GetDouble(ex, "horimetro_motor"),
                ["tipo_oleo"]                      = PayloadsUtilitario.GetStr(ex, "tipo_oleo"),
                ["quantidade_abastecida"]          = PayloadsUtilitario.GetDouble(ex, "quantidade_abastecida"),
                ["tipo_produto"]                   = PayloadsUtilitario.GetStr(ex, "tipo_produto"),
                ["volume_carga_caminhao"]          = PayloadsUtilitario.GetStr(ex, "volume_carga_caminhao"),
                ["frota_caminhao"]                 = PayloadsUtilitario.GetStr(ex, "frota_caminhao"),
                ["balanca"]                        = PayloadsUtilitario.GetStr(ex, "balanca"),
                ["box_descarga"]                   = PayloadsUtilitario.GetStr(ex, "box_descarga"),
                ["motivo"]                         = PayloadsUtilitario.GetStr(ex, "motivo"),
                ["status_inicio_abastecimento"]    = PayloadsUtilitario.GetStr(ex, "status_inicio_abastecimento"),
                ["tipo_operacao"]                  = PayloadsUtilitario.GetStr(ex, "tipo_operacao"),
                ["talhao_operacao"]                = PayloadsUtilitario.GetStr(ex, "talhao_operacao"),
                ["quantidade_arvores_cortadas"]    = PayloadsUtilitario.GetInt(ex, "quantidade_arvores_cortadas"),
                ["quantidade_mudas"]               = PayloadsUtilitario.GetInt(ex, "quantidade_mudas"),
                ["informe_producao"]               = PayloadsUtilitario.GetDouble(ex, "informe_producao"),
                ["volume_caixa_carga"]             = PayloadsUtilitario.GetStr(ex, "volume_caixa_carga"),
                ["tipo_servico"]                   = PayloadsUtilitario.GetStr(ex, "tipo_servico"),
                ["quantidade_toco"]                = PayloadsUtilitario.GetInt(ex, "quantidade_toco"),
                ["hectares"]                       = PayloadsUtilitario.GetDouble(ex, "hectares"),
                ["parada"]                         = PayloadsUtilitario.GetStr(ex, "parada"),

                ["data_registro"] = PayloadsUtilitario.FmtDate(equipDate, "sim")
            };

            envelope["tabela"] = "TimberFleet_Formulario";
            envelope["dados"]  = dados;
            return true;
        }
    }
}