

using System.Text.Json.Serialization;

namespace TF.src.Infra.Configuracoes
{
    public interface IConfigProvider
    {
        Task<RootConfig> CarregarConfiguracao(CancellationToken comando = default);
    }

    public class RootConfig
    {
        [JsonPropertyName("TF_API_BaseUrl")]
        public required string UrlTF { get; set; }

        [JsonPropertyName("PHP_API_Url")]
        public required string UrlPhp { get; set; }
        
        [JsonPropertyName("TF_Token_Url")]
        public required string UrlToken { get; set; }

        [JsonPropertyName("TF_API_Header")]
        public required Dictionary<string, string> HeadersTF { get; set; }
        
        [JsonPropertyName("PHP_header")]
        public required Dictionary<string, string> HeadersPhp { get; set; }
        
        [JsonPropertyName("TF_Token_header")]
        public required Dictionary<string, string> HeadersToken { get; set; }

        [JsonPropertyName("Credenciais")]
        public required CredenciaisMeta Credenciais { get; set; }

        [JsonPropertyName("Informacoes_Token")]
        public required TokenMeta InformacoesToken { get; set; }

        [JsonPropertyName("Tabelas")]
        public required Dictionary<string, TabelaMeta> Tabelas { get; set; }
    }

    public class TabelaMeta
    {
        [JsonPropertyName("Nome_Tabela")]
        public required string NomeTabela { get; set; }

        [JsonPropertyName("Url_Final")]
        public required string UrlFinal { get; set; }

        [JsonPropertyName("UltimaAtualizacao")]
        public required string UltimaAtualizacao { get; set; }

        [JsonPropertyName("TabelaAtiva")]
        public required bool TabelaAtiva { get; set; }
    }

    public class TokenMeta
    {
        [JsonPropertyName("Token")]
        public required string Token { get; set; }

        [JsonPropertyName("Data_Gerada")]
        public required string DataGerada { get; set; }

        [JsonPropertyName("Data_Expiracao")]
        public required string DataExpirada { get; set; }
    }

    public class CredenciaisMeta
    {
        [JsonPropertyName("grant_type")]
        public required string Grant_type { get; set; }

        [JsonPropertyName("username")]
        public required string Username { get; set; }

        [JsonPropertyName("password")]
        public required string Password { get; set; }

        [JsonPropertyName("client_id")]
        public required string Client_id { get; set; }

        [JsonPropertyName("client_secret")]
        public required string Client_secret { get; set; }
    }
}