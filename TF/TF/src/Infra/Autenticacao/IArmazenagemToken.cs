using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TF.src.Infra.Autenticacao
{
    public interface IArmazenagemToken
    {
        Task<TokenInfo?> ObterToken(CancellationToken comando = default);
        Task SalvarToken(TokenInfo token, CancellationToken comando = default);
    }

    public record TokenInfo(string Token, string Expiracao);
}