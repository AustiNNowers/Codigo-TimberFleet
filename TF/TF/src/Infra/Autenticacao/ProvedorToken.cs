using System.Globalization;
using System.Text.Json;

using TF.src.Infra.Configuracoes;
using TF.src.Infra.Logging;
using TF.src.Infra.Politica;

namespace TF.src.Infra.Autenticacao
{
    public class ProvedorToken(HttpClient http, IArmazenagemToken armazenar, RootConfig config, IConsoleLogger log) : IProvedorToken
    {
        private readonly HttpClient _http = http;
        private readonly IArmazenagemToken _armazenar = armazenar;
        private readonly RootConfig _config = config;
        private readonly IConsoleLogger _log = log;
        private readonly SemaphoreSlim _lock = new(1, 1);        
        private readonly TimeSpan _margemSeguranca = TimeSpan.FromMinutes(5);

        public async Task<string> GerarToken(CancellationToken comando = default)
        {
            var t = await _armazenar.ObterToken(comando);
            if (TokenValido(t)) return t!.Token;

            await _lock.WaitAsync(comando);
            try
            {
                t = await _armazenar.ObterToken(comando);
                if (TokenValido(t)) return t!.Token;

                await RenovarToken(comando);

                t = await _armazenar.ObterToken(comando) ?? throw new InvalidOperationException("Falha ao obter token após renovar.");
                if (!TokenValido(t))
                    throw new InvalidOperationException("Token renovado, mas expiração inválida.");

                return t.Token;
            }
            finally
            {
                _lock.Release();
            }
        }

        private bool TokenValido(TokenInfo? t)
        {
            if (t is null) return false;
            if (string.IsNullOrWhiteSpace(t.Token)) return false;

            if (!DateTime.TryParse(t.Expiracao.ToString("O"), CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var exp))
                return false;

            return exp > DateTimeOffset.UtcNow.Add(_margemSeguranca);
        }

        public async Task RenovarToken(CancellationToken comando = default)
        {
            var formato = new Dictionary<string, string>
            {
                ["grant_type"] = _config.Credenciais.Grant_type,
                ["username"] = _config.Credenciais.Username,
                ["password"] = _config.Credenciais.Password,
                ["client_id"] = _config.Credenciais.Client_id,
                ["client_secret"] = _config.Credenciais.Client_secret
            };

            using var resposta = await PoliticaRetentativa.ExecutarNovamenteRequisicao(
                _http,
                () =>
                {
                    var r = new HttpRequestMessage(HttpMethod.Post, _config.UrlToken)
                    {
                        Content = new FormUrlEncodedContent(formato)
                    };
                    foreach (var kv in _config.HeadersToken) r.Headers.TryAddWithoutValidation(kv.Key, kv.Value);

                    return r;
                },
                tentativasMaxima: 5, atrasoBase: TimeSpan.FromMilliseconds(500), timeoutPorTentativa: TimeSpan.FromSeconds(16), comando: comando, logDebug: s => _log.Aviso(s)
            );

            resposta.EnsureSuccessStatusCode();
            _log.Info("[Auth] Requisição com sucesso");

            DateTime dataExpiracao = DateTime.Now.AddDays(1).Subtract(TimeSpan.FromHours(-1));

            using var stream = await resposta.Content.ReadAsStreamAsync(comando);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: comando);

            var acesso = doc.RootElement.GetProperty("access_token").GetString() ?? "";

            await _armazenar.SalvarToken(new TokenInfo(acesso, dataExpiracao), comando);
            _log.Info("[Auth] Token salvo");
        }
    }
}