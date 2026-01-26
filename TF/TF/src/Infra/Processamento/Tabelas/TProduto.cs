using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TF.src.Infra.Modelo;
using static TF.src.Infra.Processamento.Utilidades;

namespace TF.src.Infra.Processamento.Tabelas
{
    public static class TProduto
    {
        public static ApiLinha Aplicar(ApiLinha linha)
        {
            var extra = linha.CamposExtras ??= [];

            if (TentarPegarDouble(extra, "volume", out var v))
                Set(extra, "volume", v / 10000.0);
            if (TentarPegarString(extra, "vehicle_name", out var vn))
                Set(extra, "vehicle_name", LimparColchetes(vn));

            return linha;
        }
    }
}