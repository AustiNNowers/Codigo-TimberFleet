using System.Text.Json;

namespace TF.src.Infra.Configuracoes
{
    public class ConfigJson(string caminhoArquivo) : IConfigProvider
    {
        private readonly string _caminhoArquivo = caminhoArquivo;

        private readonly JsonSerializerOptions _opcoes = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public async Task<RootConfig> CarregarConfiguracao(CancellationToken comando = default)
        {
            string json;

            if (File.Exists(_caminhoArquivo))
            {
                json = await File.ReadAllTextAsync(_caminhoArquivo, comando);
            }
            else
            {
                json = _caminhoArquivo;
            }

            var config = JsonSerializer.Deserialize<RootConfig>(json, _opcoes) ?? throw new InvalidOperationException("Falha ao carregar configuração: JSON inválido!");
            
            return config;
        }
    }
}