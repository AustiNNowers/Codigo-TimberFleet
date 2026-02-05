using TF.src.Infra.Modelo;

namespace TF.src.Infra.Processamento.Tabelas
{
    public static class TProducao
    {
        public static ApiLinha Aplicar(ApiLinha linha)
        {
            var extra = linha.CamposExtras ??= [];

            if (Utilidades.TentarPegarDouble(extra, "volume_total", out var vt))
                Utilidades.Set(extra, "volume_total", vt / 10000.0);
            if (Utilidades.TentarPegarString(extra, "vehicle_name", out var vn))
                Utilidades.Set(extra, "vehicle_name", Utilidades.LimparColchetes(vn));

            return linha;
        }
    }
}