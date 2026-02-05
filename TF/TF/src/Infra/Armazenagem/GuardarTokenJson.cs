using System.Text.Json;
using System.Text.Json.Nodes;
using TF.src.Infra.Autenticacao;

namespace TF.src.Infra.Armazenagem
{
    public class GuardarTokenJson(string caminho) : IArmazenagemToken
    {
        private readonly string _caminho = caminho ?? throw new ArgumentNullException(nameof(caminho));
        private readonly SemaphoreSlim _mutex = new(1, 1);

        private readonly JsonSerializerOptions _opcoes = new() { WriteIndented = true };

        public async Task<TokenInfo?> ObterToken(CancellationToken comando = default)
        {
            await _mutex.WaitAsync(comando);

            try
            {
                var json = await File.ReadAllTextAsync(_caminho, comando);
                var root = JsonNode.Parse(json) as JsonObject;
                var informacao = root?["Informacoes_Token"] as JsonObject;

                var token = informacao["Token"]?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(token)) return null;

                string dataExpiracao = informacao["Data_Expiracao"]?.GetValue<string>() ?? string.Empty;

                return new TokenInfo(token, dataExpiracao);
            }
            finally
            {
                _mutex.Release();
            }
        }

        public async Task SalvarToken(TokenInfo token, CancellationToken comando = default)
        {
            await _mutex.WaitAsync(comando);

            try
            {
                JsonObject root;
                if (File.Exists(_caminho))
                {
                    var json = await File.ReadAllTextAsync(_caminho, comando);
                    root = (JsonNode.Parse(json) as JsonObject) ?? [];
                }
                else
                {
                    root = [];
                }

                if (root["Informacoes_Token"] is not JsonObject informacoes)
                {
                    informacoes = [];
                    root["Informacoes_Token"] = informacoes;
                }

                informacoes["Token"] = token.Token;
                informacoes["Data_Gerada"] = token.Expiracao;
                informacoes["Data_Expiracao"] = token.Expiracao;

                var atualizacao = root.ToJsonString(_opcoes);
                
                await File.WriteAllTextAsync(_caminho, atualizacao, comando);
            }
            finally
            {
                _mutex.Release();
            }
        }
    }
}