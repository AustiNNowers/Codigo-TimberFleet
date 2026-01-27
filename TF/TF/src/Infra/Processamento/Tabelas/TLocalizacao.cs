namespace TF.src.Infra.Processamento.Tabelas
{
    public static class TLocalizacao
    {
        public static ApiLinha Aplicar(ApiLinha linha)
        {
            var extra = linha.CamposExtras ??= [];

            if (TentarPegarString(extra, "latlon", out var latlon) &&
                TentarSepararDoubles(latlon, out var lat, out var lon))
            {
                Set(extra, "latitude", lat);
                Set(extra, "longitude", lon);
                Remover(extra, "latlon");
            }

            if (TentarPegarString(extra, "vehicle_desc", out var vd))
                Set(extra, "vehicle_desc", LimparColchetes(vd));
            if (TentarPegarString(extra, "vehicle_name", out var vn))
                Set(extra, "vehicle_name", LimparColchetes(vn));

            return linha;
        }
    }
}