namespace TF.src.Infra.Processamento.Payloads
{
    public static class ContadoresPayload
    {
        public static bool TryBuild(ApiLinha linha, out Dictionary<string, object?> envelope)
        {
            envelope = [];
            var ex = linha.CamposExtras ?? [];

            var vehicleDesc = Clean(GetStr(ex, "vehicle_desc") ?? GetStr(ex, "vehicle_name"));
            var operatorName = GetStr(ex, "operator_name");
            var equipDate    = GetStr(ex, "equip_date");
            var status       = GetStr(ex, "vehicle_status_desc") ?? GetStr(ex, "status_desc");

            var dados = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["id_ident"]        = JoinId(vehicleDesc, operatorName, equipDate),
                ["prefixo"]         = (vehicleDesc ?? "").Replace("VT-", "", StringComparison.OrdinalIgnoreCase).Trim(),
                ["nome_operador"]   = operatorName,
                ["data_contadores"] = FmtDate(equipDate),
                ["operacao_status"] = status,

                ["odometro_motor"]        = GetDouble(ex, "odometer_motor")?.Round(2),
                ["odometro_operacao"]     = GetDouble(ex, "odometer_operation")?.Round(2),
                ["odometro_implemento"]   = GetDouble(ex, "odometer_implement")?.Round(2),
                ["odometro_rodante"]      = GetDouble(ex, "odometer_travel")?.Round(2),

                ["horimetro_operacao"]    = GetDouble(ex, "engine_hourmeter"),
                ["horimetro_motor"]       = GetDouble(ex, "operation_hourmeter"),
                ["horimetro_implemento"]  = GetDouble(ex, "implement_hourmeter"),
                ["horimetro_rodante"]     = GetDouble(ex, "travel_hourmeter"),

                ["ativacao_motor"]        = GetInt(ex, "engine_activation"),
                ["ativacao_operacao"]     = GetInt(ex, "operation_activation"),
                ["ativacao_implemento"]   = GetInt(ex, "implement_activation"),
                ["ativacao_rodante"]      = GetInt(ex, "travel_activation"),

                ["tempo_movimentacao_motor"]      = GetDouble(ex, "moving_time_motor"),
                ["tempo_movimentacao_operacao"]   = GetDouble(ex, "moving_time_operation"),
                ["tempo_movimentacao_implemento"] = GetDouble(ex, "moving_time_implement"),
                ["tempo_movimentacao_rodante"]    = GetDouble(ex, "moving_time_travel"),

                ["diferenca_odometro_motor"]      = GetDouble(ex, "odometer_motor_diff"),
                ["diferenca_odometro_operacao"]   = GetDouble(ex, "odometer_operation_diff"),
                ["diferenca_odometro_implemento"] = GetDouble(ex, "odometer_implement_diff"),
                ["diferenca_odometro_rodante"]    = GetDouble(ex, "odometer_travel_diff"),

                ["diferenca_horimetro_motor"]     = GetDouble(ex, "operation_hourmeter_diff"),
                ["diferenca_horimetro_operacao"]  = GetDouble(ex, "engine_hourmeter_diff"),
                ["diferenca_horimetro_implemento"]= GetDouble(ex, "implement_hourmeter_diff"),
                ["diferenca_horimetro_rodante"]   = GetDouble(ex, "travel_hourmeter_diff"),

                ["diferenca_ativacao_motor"]      = GetInt(ex, "engine_activation_diff"),
                ["diferenca_ativacao_operacao"]   = GetInt(ex, "operation_activation_diff"),
                ["diferenca_ativacao_implemento"] = GetInt(ex, "implement_activation_diff"),
                ["diferenca_ativacao_rodante"]    = GetInt(ex, "travel_activation_diff"),

                ["diferenca_tempo_movimentacao_motor"]      = GetDouble(ex, "moving_time_motor_diff"),
                ["diferenca_tempo_movimentacao_operacao"]   = GetDouble(ex, "moving_time_operation_diff"),
                ["diferenca_tempo_movimentacao_implemento"] = GetDouble(ex, "moving_time_implement_diff"),
                ["diferenca_tempo_movimentacao_rodante"]    = GetDouble(ex, "moving_time_travel_diff"),

                ["data_registro"] = FmtDate(equipDate, "sim")
            };

            envelope["tabela"] = "TimberFleet_Contadores";
            envelope["dados"]  = dados;
            return true;
        }
    }
}
