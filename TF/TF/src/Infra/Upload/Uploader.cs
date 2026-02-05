using System.Net.Http.Headers;
using System.IO.Compression;
using System.Text;

using TF.src.Infra.Configuracoes;
using TF.src.Infra.Lote;
using TF.src.Infra.Logging;
using TF.src.Infra.Politica;

namespace TF.src.Infra.Upload
{
    public class Uploader(HttpClient http, RootConfig configuracoes, IConsoleLogger log) : IUploader
    {
        private readonly HttpClient _http = http;
        private readonly IConsoleLogger _log = log;
        private readonly string _urlPhp = configuracoes.UrlPhp;
        private readonly IReadOnlyDictionary<string, string> _headers = configuracoes.HeadersPhp;

        public async Task UploadPhp(LoteadorPayload lote, CancellationToken comando = default)
        {
            _log.Info("[Uploader] Iniciando a entrega dos lotes...");
            ArgumentNullException.ThrowIfNull(lote);
            if (lote.BytesComprimidos is null || lote.BytesComprimidos.Length == 0) throw new ArgumentException("Payload vazio.", nameof(lote));

            _log.Info("[Uploader] Iniciando requisição de entrega dos dados...");
            _log.Info($"[Uploader] Enviando Lote: {lote.Quantidade} linhas | " +
                      $"Tamanho: {lote.TamanhoBytes} bytes (Gzip) | " +
                      $"Range: {lote.DataInicio ?? "?"} até {lote.DataFim ?? "?"}");
            
            using var resposta = await PoliticaRetentativa.ExecutarNovamenteRequisicao(
                    _http, () => CriarRequisicao(lote),
                    tentativasMaxima: 5, atrasoBase: TimeSpan.FromMilliseconds(1000), timeoutPorTentativa: TimeSpan.FromSeconds(300), comando: comando, logDebug: s => _log.Aviso(s)
            );

            var body = await resposta.Content.ReadAsStringAsync(comando);

            if (!resposta.IsSuccessStatusCode)
            {
                _log.Erro($"[Uploader] FALHA PHP {(int)resposta.StatusCode}: {body}");
                _log.SalvarLogs(body, $"ERRO_Upload_{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}");
                resposta.EnsureSuccessStatusCode();
            }
            else
            {
                var resumo = body.Length > 200 ? body[..200] + "..." : body;
                _log.Info($"[Uploader] Sucesso! Resposta: {resumo}");
            }

            _log.Info($"[Uploader] Enviado com sucesso | Total de linhas={lote.Quantidade} - Peso gzip={lote.TamanhoBytes} bytes");
        }

        public Task UploadPhp(LoteadorPayload lote, CancellationToken comando = default)
        {
            throw new NotImplementedException();
        }

        private HttpRequestMessage CriarRequisicao(LoteadorPayload lote)
        {
            var r = new HttpRequestMessage(HttpMethod.Post, _urlPhp);
            foreach (var (k, v) in _headers) r.Headers.TryAddWithoutValidation(k, v);

            r.Content = new ByteArrayContent(lote.BytesComprimidos);
            r.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-ndjson")
            {
                CharSet = "utf-8"
            };
            r.Content.Headers.ContentEncoding.Add("gzip");
            
            return r;
        }
    }
}