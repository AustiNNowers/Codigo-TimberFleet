using TF.src.Infra.Modelo;

namespace TF.src.Infra.Processamento.Tabelas
{
    public static class TLocalizacao
    {
        public static ApiLinha Aplicar(ApiLinha linha)
        {
            var extra = linha.CamposExtras ??= [];

            if (Utilidades.TentarPegarString(extra, "latlon", out var latlon) &&
                Utilidades.TentarSepararDoubles(latlon, out var lat, out var lon))
            {
                Utilidades.Set(extra, "latitude", lat);
                Utilidades.Set(extra, "longitude", lon);
                Utilidades.Remover(extra, "latlon");
            }

            if (Utilidades.TentarPegarString(extra, "vehicle_desc", out var vd))
                Utilidades.Set(extra, "vehicle_desc", Utilidades.LimparColchetes(vd));
            if (Utilidades.TentarPegarString(extra, "vehicle_name", out var vn))
                Utilidades.Set(extra, "vehicle_name", Utilidades.LimparColchetes(vn));

            return linha;
        }
    }
}