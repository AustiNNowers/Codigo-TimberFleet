using TF.src.Infra.Modelo;

namespace TF.src.Infra.Processamento.Tabelas
{
    public static class TProduto
    {
        public static ApiLinha Aplicar(ApiLinha linha)
        {
            var extra = linha.CamposExtras ??= [];

            if (Utilidades.TentarPegarDouble(extra, "volume", out var v))
                Utilidades.Set(extra, "volume", v / 10000.0);
            if (Utilidades.TentarPegarString(extra, "vehicle_name", out var vn))
                Utilidades.Set(extra, "vehicle_name", Utilidades.LimparColchetes(vn));

            return linha;
        }
    }
}