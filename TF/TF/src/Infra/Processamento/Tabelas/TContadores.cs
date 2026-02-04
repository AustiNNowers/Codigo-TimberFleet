using System.Text.Json;

namespace TF.src.Infra.Processamento.Tabelas
{
    public class TContadores
    {
        private static readonly CultureInfo _inv = CultureInfo.InvariantCulture;

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

            if (Utilidades.TentarPegarString(extra, "vehicle_desc", out var vd))
                Utilidades.Set(extra, "vehicle_desc", Utilidades.LimparColchetes(vd));
            if (Utilidades.TentarPegarString(extra, "vehicle_name", out var vn))
                Utilidades.Set(extra, "vehicle_name", Utilidades.LimparColchetes(vn));

            return linha;
        }

        private static void DividirPontoVirgula(Dictionary<string, JsonElement> extra, string chaveFonte,
        (string chaveExterna, string _)[] alvos)
        {
            if (!Utilidades.TentarPegarString(extra, chaveFonte, out var str) || string.IsNullOrWhiteSpace(str)) return;

            var span = str.AsSpan();
            int alvoIdx = 0;
            int start = 0;

            while (alvoIdx < alvos.Length)
            {
                int end = span.Slice(start).IndexOf(';');
                ReadOnlySpan<char> segmento;

                if (end == -1)
                {
                    segmento = span.Slice(start);
                }
                else
                {
                    segmento = span.Slice(start, end);
                }

                if (double.TryParse(segmento, NumberStyles.Float, _inv, out var valor))
                {
                     Utilidades.Set(extra, alvos[alvoIdx].chaveExterna, valor);
                }
                else
                {
                    var s = segmento.ToString().Replace(',', '.');
                    if (double.TryParse(s, NumberStyles.Float, _inv, out valor))
                    {
                        Utilidades.Set(extra, alvos[alvoIdx].chaveExterna, valor);
                    }
                }

                if (end == -1) break;
                
                start += end + 1;
                alvoIdx++;
            }
        }
    }
}