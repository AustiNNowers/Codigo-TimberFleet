using TF.src.Infra.Autenticacao;

namespace TF.src.Infra.Armazenagem
{
    public class GerenciadorArmazenagemToken(IArmazenagemToken memoria, IArmazenagemToken json) : IArmazenagemToken
    {
        private readonly IArmazenagemToken _memoria = memoria;
        private readonly IArmazenagemToken _json = json;

        public async Task<TokenInfo?> ObterToken(CancellationToken comando = default)
        {
            var token = await _memoria.ObterToken(comando);
            if (token is not null && token.Expiracao > DateTime.Now) return token;

            token = await _json.ObterToken(comando);
            if (token is not null && token.Expiracao > DateTime.Now)
            {
                await _memoria.SalvarToken(token, comando);
                return token;
            }

            return null;
        }

        public async Task SalvarToken(TokenInfo token, CancellationToken comando = default)
        {
            var s1 = _memoria.SalvarToken(token, comando);
            var s2 = _json.SalvarToken(token, comando);

            await Task.WhenAll(s1, s2);
        }
    }
}