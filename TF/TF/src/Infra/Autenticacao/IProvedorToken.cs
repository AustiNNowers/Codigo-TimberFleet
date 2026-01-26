using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TF.src.Infra.Autenticacao
{
    public interface IProvedorToken
    {
        Task<string> GerarToken(CancellationToken comando = default);
        Task RenovarToken(CancellationToken comando = default);
    }
}