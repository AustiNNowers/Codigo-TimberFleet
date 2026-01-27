using System.Net.Http.Headers;
using System.IO.Compression;
using System.Text;

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
            _log.Info($"[Uploader] Lote: {lote.ToString}");
            _log.Info($"[Uploader] LotesConvertidos: {ObterAmostraDosDados(lote)}");
            
            using var resposta = await PoliticaRetentativa.ExecutarNovamenteRequisicao(
                    _http, () =>
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
                    },
                    tentativasMaxima: 5, atrasoBase: TimeSpan.FromMilliseconds(1000), timeoutPorTentativa: TimeSpan.FromSeconds(300), comando: comando, logDebug: s => _log.Aviso(s)
            );

            var body = await resposta.Content.ReadAsStringAsync(comando);
            _log.Info($"[Uploader] Resposta do PHP: {body}");

            _log.SalvarLogs(body, "Uploader_" + DateTime.UtcNow.ToString().Replace("/", "-").Replace(":", "-"));

            _log.Info("[Uploader] Verificando se a requisição foi um sucesso...");
            resposta.EnsureSuccessStatusCode();

            _log.Info($"[Uploader] Enviado com sucesso | Total de linhas={lote.Quantidade} - Peso gzip={lote.TamanhoBytes} bytes");
        }

        private static string ObterAmostraDosDados(LoteadorPayload lotes, int maximoCaracteres = 10000)
        {
            if (lotes.BytesComprimidos == null || lotes.BytesComprimidos.Length == 0)
            {
                return "[Lote Vazio]";
            }

            try
            {
                using var streamComprimido = new MemoryStream(lotes.BytesComprimidos);
                
                using var streamDescomprimido = new MemoryStream();
                
                using (var descompressor = new GZipStream(streamComprimido, CompressionMode.Decompress))
                {
                    descompressor.CopyTo(streamDescomprimido);
                }

                string dadosCompletos = Encoding.UTF8.GetString(streamDescomprimido.ToArray());

                if (dadosCompletos.Length > maximoCaracteres)
                {
                    return dadosCompletos.Substring(0, maximoCaracteres) + "... [truncado]";
                }

                return dadosCompletos;
            }
            catch (Exception ex)
            {
                return $"[Falha ao descomprimir amostra: {ex.Message}]";
            }
        }
    }
}