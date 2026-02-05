using TF.src.Infra.Modelo;

namespace TF.src.Infra.Processamento.Payloads
{
    public static class ApontamentosPayload
    {
        public static bool TryBuild(ApiLinha linha, out Dictionary<string, object?> envelope)
        {
            envelope = new();
            var ex = linha.CamposExtras ?? new();

            var vehicleDesc = PayloadsUtilitario.Clean(PayloadsUtilitario.GetStr(ex, "vehicle_desc") ?? PayloadsUtilitario.GetStr(ex, "vehicle_name"));
            var operatorName = PayloadsUtilitario.GetStr(ex, "operator_name");
            var operatorCode = PayloadsUtilitario.GetStr(ex, "operator_code");
            var startDate = PayloadsUtilitario.GetStr(ex, "start_date");
            var finalDate = PayloadsUtilitario.GetStr(ex, "final_date");
            var status = PayloadsUtilitario.GetStr(ex, "status_desc");
            var formTitle = PayloadsUtilitario.GetStr(ex, "form_title");

            var dados = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["id_ident"] = PayloadsUtilitario.JoinId(vehicleDesc, operatorName, startDate),
                ["prefixo"] = (vehicleDesc ?? "").Replace("VT-", "", StringComparison.OrdinalIgnoreCase).Trim(),
                ["nome_operador"] = operatorName,
                ["codigo_operador"] = operatorCode,
                ["data_inicio"] = PayloadsUtilitario.FmtDate(startDate),
                ["data_final"] = PayloadsUtilitario.FmtDate(finalDate),
                ["inicio_horimetro_motor"] = PayloadsUtilitario.GetDouble(ex, "start_engine_hourmeter"),
                ["final_horimetro_motor"] = PayloadsUtilitario.GetDouble(ex, "final_engine_hourmeter"),
                ["operacao_status"] = status,
                ["titulo_formulario"] = formTitle,

                ["matricula_operador"] = PayloadsUtilitario.GetStr(ex, "matricula_operador"),
                ["matricula_comboista"] = PayloadsUtilitario.GetStr(ex, "matricula_comboista"),
                ["matricula_mecanico"] = PayloadsUtilitario.GetStr(ex, "matricula_mecanico"),
                ["tipo_falha"] = PayloadsUtilitario.GetStr(ex, "tipo_falha"),
                ["atividade_anterior_apropriacao"] = PayloadsUtilitario.GetStr(ex, "atividade_anterior_apropriacao"),
                ["avaliacao"] = PayloadsUtilitario.GetStr(ex, "tipo_avaliacao") ?? PayloadsUtilitario.GetStr(ex, "avaliacao"),
                ["equipamento_disponivel"] = PayloadsUtilitario.GetStr(ex, "equipamento_disponivel"),
                ["motivo_indisponibilidade"] = PayloadsUtilitario.GetStr(ex, "motivo_indisponibilidade"),
                ["horimetro_motor"] = PayloadsUtilitario.GetDouble(ex, "horimetro_motor"),
                ["tipo_oleo"] = PayloadsUtilitario.GetStr(ex, "tipo_oleo"),
                ["quantidade_abastecida"] = PayloadsUtilitario.GetDouble(ex, "quantidade_abastecida"),
                ["tipo_produto"] = PayloadsUtilitario.GetStr(ex, "tipo_produto"),
                ["volume_carga_caminhao"] = PayloadsUtilitario.GetStr(ex, "volume_carga_caminhao"),
                ["frota_caminhao"] = PayloadsUtilitario.GetStr(ex, "frota_caminhao"),
                ["balanca"] = PayloadsUtilitario.GetInt(ex, "balanca"),
                ["box_descarga"] = PayloadsUtilitario.GetStr(ex, "box_descarga"),
                ["motivo"] = PayloadsUtilitario.GetStrWithoutFL(ex, "motivo"),
                ["status_inicio_abastecimento"] = PayloadsUtilitario.GetStr(ex, "status_inicio_abastecimento"),
                ["tipo_operacao"] = PayloadsUtilitario.GetStr(ex, "tipo_operacao"),
                ["talhao_operacao"] = PayloadsUtilitario.GetStr(ex, "talhao_operacao"),
                ["quantidade_arvores_cortadas"] = PayloadsUtilitario.GetInt(ex, "quantidade_arvores_cortadas"),
                ["quantidade_mudas"] = PayloadsUtilitario.GetInt(ex, "quantidade_mudas"),
                ["informe_producao"] = PayloadsUtilitario.GetDouble(ex, "informe_producao"),
                ["volume_caixa_carga"] = PayloadsUtilitario.GetStr(ex, "volume_caixa_carga"),
                ["tipo_servico"] = PayloadsUtilitario.GetStr(ex, "tipo_servico"),
                ["quantidade_toco"] = PayloadsUtilitario.GetInt(ex, "quantidade_toco"),
                ["hectares"] = PayloadsUtilitario.GetDouble(ex, "hectares"),
                ["parada"] = PayloadsUtilitario.GetStr(ex, "parada"),

                ["data_registro"] = PayloadsUtilitario.FmtDate(startDate, "sim")
            };

            envelope["tabela"] = "TimberFleet_Apontamentos";
            envelope["dados"] = dados;
            return true;
        }
    }
}