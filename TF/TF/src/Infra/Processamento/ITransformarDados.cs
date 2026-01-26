using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TF.src.Infra.Modelo;

namespace TF.src.Infra.Processamento
{
    public interface ITransformarDados
    {
        IEnumerable<ApiLinha> Transformar(string tabelaChave, IEnumerable<ApiLinha> linhas);
    }
}