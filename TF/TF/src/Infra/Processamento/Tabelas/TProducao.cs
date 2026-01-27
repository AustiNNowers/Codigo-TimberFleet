namespace TF.src.Infra.Processamento.Tabelas
{
    public static class TProducao
    {
        public static ApiLinha Aplicar(ApiLinha linha)
        {
            var extra = linha.CamposExtras ??= [];

            if (TentarPegarDouble(extra, "volume_total", out var vt))
                Set(extra, "volume_total", vt / 10000.0);
            if (TentarPegarString(extra, "vehicle_name", out var vn))
                Set(extra, "vehicle_name", LimparColchetes(vn));

            return linha;
        }
    }
}