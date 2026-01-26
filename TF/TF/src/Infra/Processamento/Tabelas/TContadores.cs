using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography.Xml;
using System.Text.Json;
using System.Threading.Tasks;

using TF.src.Infra.Modelo;
using static TF.src.Infra.Processamento.Utilidades;

namespace TF.src.Infra.Processamento.Tabelas
{
    public class TContadores
    {
        public static ApiLinha Aplicar(ApiLinha linha)
        {
            var extra = linha.CamposExtras ??= [];

            DividirPontoVirgula(extra, "odometer",
            [
            ("odometer_motor", "odometer_motor"),
            ("odometer_operation", "odometer_operation"),
            ("odometer_implement", "odometer_implement"),
            ("odometer_travel", "odometer_travel"),
            ("odometro_nao_usado", "odometro_nao_usado")
            ]);

            DividirPontoVirgula(extra, "moving_time",
            [
                ("moving_time_motor", "moving_time_motor"),
                ("moving_time_operation", "moving_time_operation"),
                ("moving_time_implement", "moving_time_implement"),
                ("moving_time_travel", "moving_time_travel"),
                ("tempo_movimentacao_nao_usado", "tempo_movimentacao_nao_usado")
            ]);

            DividirPontoVirgula(extra, "odometer_diff",
            [
                ("odometer_motor_diff", "odometer_motor_diff"),
                ("odometer_operation_diff", "odometer_operation_diff"),
                ("odometer_implement_diff", "odometer_implement_diff"),
                ("odometer_travel_diff", "odometer_travel_diff"),
                ("odometro_nao_usado_diff", "odometro_nao_usado_diff")
            ]);

            DividirPontoVirgula(extra, "moving_time_diff",
            [
                ("moving_time_motor_diff", "moving_time_motor_diff"),
                ("moving_time_operation_diff", "moving_time_operation_diff"),
                ("moving_time_implement_diff", "moving_time_implement_diff"),
                ("moving_time_travel_diff", "moving_time_travel_diff"),
                ("tempo_movimentacao_nao_usado_diff", "tempo_movimentacao_nao_usado_diff")
            ]);

            if (TentarPegarString(extra, "vehicle_desc", out var vd))
                Set(extra, "vehicle_desc", LimparColchetes(vd));
            if (TentarPegarString(extra, "vehicle_name", out var vn))
                Set(extra, "vehicle_name", LimparColchetes(vn));

            return linha;
        }

        private static void DividirPontoVirgula(Dictionary<string, JsonElement> extra, string chaveFonte,
            (string chaveExterna, string _)[] alvos)
        {
            if (!TentarPegarString(extra, chaveFonte, out var str) || string.IsNullOrWhiteSpace(str)) return;

            var partes = str.Split(';');
            for (int i = 0; i < alvos.Length && i < partes.Length; i++)
            {
                if (double.TryParse(partes[i].Replace(',', '.'), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var valor))
                {
                    Set(extra, alvos[i].chaveExterna, valor);
                }
            }
        }
    }
}