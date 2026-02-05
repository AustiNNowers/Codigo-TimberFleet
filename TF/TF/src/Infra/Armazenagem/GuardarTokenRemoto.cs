using TF.src.Infra.Autenticacao;
using TF.src.Infra.Armazenagem;

namespace TF.src.Infra.Armazenagem
{
    public class GuardarTokenRemoto : IArmazenagemToken
    {
        private volatile TokenInfo? _token;

        public Task<TokenInfo?> ObterToken(CancellationToken comando = default) => Task.FromResult(_token);

        public Task SalvarToken(TokenInfo token, CancellationToken comando = default)
        {
            _token = token;
            return Task.CompletedTask;
        }
    }
}