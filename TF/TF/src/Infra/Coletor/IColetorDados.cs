using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using TF.src.Infra.Modelo;

namespace TF.src.Infra.Coletor
{
    public interface IColetorDados
    {
        IAsyncEnumerable<ApiLinha> ColetarDados(
            string urlFinal,
            DateTimeOffset dataAtual,
            CancellationToken comando = default
        );
    }
}