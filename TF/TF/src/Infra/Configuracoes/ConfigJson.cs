using System.Text.Json;

namespace TF.src.Infra.Configuracoes
{
    public class ConfigJson(string caminhoArquivo) : IConfigProvider
    {
        private readonly string _caminhoArquivo = caminhoArquivo;

        private readonly JsonSerializerOptions _opcoes = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        public async Task<RootConfig> CarregarConfiguracao(CancellationToken comando = default)
        {
            if (File.Exists(_caminhoArquivo))
            {
                using var stream = File.OpenRead(_caminhoArquivo);
                var configArquivo = await JsonSerializer.DeserializeAsync<RootConfig>(stream, _opcoes, comando);
                return configArquivo ?? throw new InvalidOperationException($"O arquivo '{_caminhoArquivo}' está vazio ou inválido.");
            }

            if (_caminhoArquivo.TrimStart().StartsWith("{"))
            {
                var configString = JsonSerializer.Deserialize<RootConfig>(_caminhoArquivo, _opcoes);
                return configString ?? throw new InvalidOperationException("O JSON fornecido via string é inválido.");
            }

            throw new FileNotFoundException($"Arquivo de configuração não encontrado e o conteúdo não parece ser JSON válido: {_caminhoArquivo}");
        }
    }
}