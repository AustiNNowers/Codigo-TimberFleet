using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TF.src.Infra.Autenticacao;

namespace TF.src.Infra.Armazenagem
{
    public class GuardarTokenRemoto : IArmazenagemToken
    {
        private TokenInfo? _token;

        public Task<TokenInfo?> ObterToken(CancellationToken comando = default) => Task.FromResult(_token);

        public Task SalvarToken(TokenInfo token, CancellationToken comando = default)
        {
            _token = token;
            return Task.CompletedTask;
        }
    }
}