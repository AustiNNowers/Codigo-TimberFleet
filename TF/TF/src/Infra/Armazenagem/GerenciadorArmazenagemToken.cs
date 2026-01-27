namespace TF.src.Infra.Armazenagem
{
    public class GerenciadorArmazenagemToken(IArmazenagemToken memoria, IArmazenagemToken json) : IArmazenagemToken
    {
        private readonly IArmazenagemToken _memoria = memoria;
        private readonly IArmazenagemToken _json = json;

        public async Task<TokenInfo?> ObterToken(CancellationToken comando = default)
        {
            var token = await _memoria.ObterToken(comando);
            if (token is not null) return token;

            token = await _json.ObterToken(comando);
            if (token is not null)
            {
                await _memoria.SalvarToken(token, comando);
            }

            return token;
        }

        public async Task SalvarToken(TokenInfo token, CancellationToken comando = default)
        {
            await _memoria.SalvarToken(token, comando);
            await _json.SalvarToken(token, comando);
        }
    }
}