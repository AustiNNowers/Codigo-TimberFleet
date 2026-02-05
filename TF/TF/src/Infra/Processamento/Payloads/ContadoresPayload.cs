using TF.src.Infra.Modelo;

namespace TF.src.Infra.Processamento.Payloads
{
    public static class ContadoresPayload
    {
        public static bool TryBuild(ApiLinha linha, out Dictionary<string, object?> envelope)
        {
            envelope = [];
            var ex = linha.CamposExtras ?? [];

            var vehicleDesc = PayloadsUtilitario.Clean(PayloadsUtilitario.GetStr(ex, "vehicle_desc") ?? PayloadsUtilitario.GetStr(ex, "vehicle_name"));
            var operatorName = PayloadsUtilitario.GetStr(ex, "operator_name");
            var equipDate    = PayloadsUtilitario.GetStr(ex, "equip_date");
            var status       = PayloadsUtilitario.GetStr(ex, "vehicle_status_desc") ?? PayloadsUtilitario.GetStr(ex, "status_desc");

            var dados = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["id_ident"]        = PayloadsUtilitario.JoinId(vehicleDesc, operatorName, equipDate),
                ["prefixo"]         = (vehicleDesc ?? "").Replace("VT-", "", StringComparison.OrdinalIgnoreCase).Trim(),
                ["nome_operador"]   = operatorName,
                ["data_contadores"] = PayloadsUtilitario.FmtDate(equipDate),
                ["operacao_status"] = status,

                ["odometro_motor"]        = PayloadsUtilitario.GetDouble(ex, "odometer_motor")?.Round(2),
                ["odometro_operacao"]     = PayloadsUtilitario.GetDouble(ex, "odometer_operation")?.Round(2),
                ["odometro_implemento"]   = PayloadsUtilitario.GetDouble(ex, "odometer_implement")?.Round(2),
                ["odometro_rodante"]      = PayloadsUtilitario.GetDouble(ex, "odometer_travel")?.Round(2),

                ["horimetro_operacao"]    = PayloadsUtilitario.GetDouble(ex, "engine_hourmeter"),
                ["horimetro_motor"]       = PayloadsUtilitario.GetDouble(ex, "operation_hourmeter"),
                ["horimetro_implemento"]  = PayloadsUtilitario.GetDouble(ex, "implement_hourmeter"),
                ["horimetro_rodante"]     = PayloadsUtilitario.GetDouble(ex, "travel_hourmeter"),

                ["ativacao_motor"]        = PayloadsUtilitario.GetInt(ex, "engine_activation"),
                ["ativacao_operacao"]     = PayloadsUtilitario.GetInt(ex, "operation_activation"),
                ["ativacao_implemento"]   = PayloadsUtilitario.GetInt(ex, "implement_activation"),
                ["ativacao_rodante"]      = PayloadsUtilitario.GetInt(ex, "travel_activation"),

                ["tempo_movimentacao_motor"]      = PayloadsUtilitario.GetDouble(ex, "moving_time_motor"),
                ["tempo_movimentacao_operacao"]   = PayloadsUtilitario.GetDouble(ex, "moving_time_operation"),
                ["tempo_movimentacao_implemento"] = PayloadsUtilitario.GetDouble(ex, "moving_time_implement"),
                ["tempo_movimentacao_rodante"]    = PayloadsUtilitario.GetDouble(ex, "moving_time_travel"),

                ["diferenca_odometro_motor"]      = PayloadsUtilitario.GetDouble(ex, "odometer_motor_diff"),
                ["diferenca_odometro_operacao"]   = PayloadsUtilitario.GetDouble(ex, "odometer_operation_diff"),
                ["diferenca_odometro_implemento"] = PayloadsUtilitario.GetDouble(ex, "odometer_implement_diff"),
                ["diferenca_odometro_rodante"]    = PayloadsUtilitario.GetDouble(ex, "odometer_travel_diff"),

                ["diferenca_horimetro_motor"]     = PayloadsUtilitario.GetDouble(ex, "operation_hourmeter_diff"),
                ["diferenca_horimetro_operacao"]  = PayloadsUtilitario.GetDouble(ex, "engine_hourmeter_diff"),
                ["diferenca_horimetro_implemento"]= PayloadsUtilitario.GetDouble(ex, "implement_hourmeter_diff"),
                ["diferenca_horimetro_rodante"]   = PayloadsUtilitario.GetDouble(ex, "travel_hourmeter_diff"),

                ["diferenca_ativacao_motor"]      = PayloadsUtilitario.GetInt(ex, "engine_activation_diff"),
                ["diferenca_ativacao_operacao"]   = PayloadsUtilitario.GetInt(ex, "operation_activation_diff"),
                ["diferenca_ativacao_implemento"] = PayloadsUtilitario.GetInt(ex, "implement_activation_diff"),
                ["diferenca_ativacao_rodante"]    = PayloadsUtilitario.GetInt(ex, "travel_activation_diff"),

                ["diferenca_tempo_movimentacao_motor"]      = PayloadsUtilitario.GetDouble(ex, "moving_time_motor_diff"),
                ["diferenca_tempo_movimentacao_operacao"]   = PayloadsUtilitario.GetDouble(ex, "moving_time_operation_diff"),
                ["diferenca_tempo_movimentacao_implemento"] = PayloadsUtilitario.GetDouble(ex, "moving_time_implement_diff"),
                ["diferenca_tempo_movimentacao_rodante"]    = PayloadsUtilitario.GetDouble(ex, "moving_time_travel_diff"),

                ["data_registro"] = PayloadsUtilitario.FmtDate(equipDate, "sim")
            };

            envelope["tabela"] = "TimberFleet_Contadores";
            envelope["dados"]  = dados;
            return true;
        }
    }
}
