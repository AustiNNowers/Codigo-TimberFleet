using System.Globalization;
using System.Net;
using System.Text.Json;
using TF.src.Infra.Autenticacao;
using TF.src.Infra.Logging;
using TF.src.Infra.Modelo;
using TF.src.Infra.Politica;
using TF.src.Infra.Processamento;

namespace TF.src.Infra.Coletor
{
    public class ApiCliente
    (
        HttpClient http,
        IProvedorToken provedorToken,
        IConsoleLogger log,
        TimeSpan intervalo,
        string urlBase,
        IReadOnlyDictionary<string, string> headers,
        TimeSpan tamanhoJanelaBusca,
        TimeSpan margemVerificacao
    ) : IColetorDados
    {
        private readonly JsonSerializerOptions _opcoesSerializer = new()
        {
            PropertyNameCaseInsensitive = true
        };
        private readonly JsonDocumentOptions _opcoesDocument = new()
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        };

        private static readonly string[] _candidatosData = ["updated_at", "updatedAt", "equip_date", "final_date", "start_date", "date", "data"];

        private readonly TimeSpan _tamanhoJanelaBuscaHMS = TimeSpan.FromHours(23) + TimeSpan.FromMinutes(59) + TimeSpan.FromSeconds(59);
        private readonly HttpClient _http = http;
        private readonly IProvedorToken _provedorToken = provedorToken;
        private readonly IConsoleLogger _log = log;
        private readonly TimeSpan _intervalo = intervalo;
        private readonly TimeSpan _tamanhoJanelaBusca = tamanhoJanelaBusca;
        private readonly TimeSpan _margemVerificacaoD = margemVerificacao;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly string _urlBase = urlBase;
        private readonly IReadOnlyDictionary<string, string> _headers = headers;
        private long _lastTicks;

        public async IAsyncEnumerable<ApiLinha> ColetarDados(
            string urlFinal,
            DateTimeOffset dataBusca,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken comando = default
        )
        {
            DateTimeOffset dataAtual = DateTimeOffset.UtcNow;
            DateTimeOffset dataInicio = NormalizarData(dataBusca) - _margemVerificacaoD;
            _log.Info($"[ApiCliente] DataInicio: {dataInicio} | Iniciando função...");

            while (dataInicio < dataAtual)
            {
                _log.Info($"[ApiCliente] Iniciando o tempo de espera de Requisição");
                await LimiteRequisicao(comando);

                DateTimeOffset dataFimCalculada = dataInicio + _tamanhoJanelaBusca + _tamanhoJanelaBuscaHMS;
                DateTimeOffset dataFim = dataFimCalculada > dataAtual ? dataAtual : dataFimCalculada;

                _log.Info($"[ApiCliente] Criando Url para requisição...");
                var url = ConstrutorUrl(urlFinal, dataInicio, dataFim);

                _log.Info($"[ApiCliente] Verificar se token é valido...");
                var token = await _provedorToken.GerarToken(comando);

                _log.Info($"[ApiCliente] Criando corpo da requisição... | Url: {url} - Headers: {_headers.Values}");
                Func<HttpRequestMessage> criarRequisicao = () =>
                {
                    var requisicao = new HttpRequestMessage(HttpMethod.Get, url);
                    foreach (var (k, v) in _headers) requisicao.Headers.TryAddWithoutValidation(k, v);

                    requisicao.Headers.Remove("authorization");
                    requisicao.Headers.TryAddWithoutValidation("authorization", $"Bearer {token}");
                    return requisicao;
                };

                _log.Info($"[ApiCliente] Requisitando...");
                
                HttpResponseMessage resposta;

                try 
                {
                    resposta = await PoliticaRetentativa.ExecutarNovamenteRequisicao(
                        _http, criarRequisicao, 5, TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(300), comando, s => _log.Aviso(s));
                }
                catch (Exception ex)
                {
                    _log.Erro($"[ApiCliente] Falha fatal na requisição: {ex.Message}");
                    throw;
                }

                if (resposta.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _log.Aviso("[ApiCliente] 401 Não autorizado | Renovando token e tentando novamente...");
                    resposta.Dispose();
                    token = await _provedorToken.GerarToken(comando);

                    resposta = await PoliticaRetentativa.ExecutarNovamenteRequisicao(
                        _http, criarRequisicao, tentativasMaxima: 5, atrasoBase: TimeSpan.FromMilliseconds(500), timeoutPorTentativa: TimeSpan.FromSeconds(300), comando: comando, logDebug: s => _log.Aviso(s));
                }

                resposta.EnsureSuccessStatusCode();

                _log.Info($"[ApiCliente] Tratando o json...");
                using var stream = await resposta.Content.ReadAsStreamAsync(comando);
                using var doc = await JsonDocument.ParseAsync(stream, _opcoesDocument, comando);

                _log.SalvarLogs(stream.ToString(), "ApiCliente_" + urlFinal.Replace("?", "") + "_" + DateTime.UtcNow.ToString().Replace("/", "-").Replace(":", "-"));
                _log.Info($"[ApiCliente] Conexão foi feita com sucesso! Codigo de retorno: {resposta.StatusCode} | Resposta de Retorno: {resposta.Content}");

                if (!TentarPegarArray(doc.RootElement, out var arr))
                {
                    _log.Aviso("[ApiCliente] Resposta vinda não é um aray e nem contém 'data'/'items'");
                    _log.Info($"[ApiCliente] A informação veio dessa maneira: {arr}");
                    dataInicio += _tamanhoJanelaBusca;
                    continue;
                }

                var linhas = new List<ApiLinha>(capacity: Math.Max(64, arr.GetArrayLength()));

                foreach (var elemento in arr.EnumerateArray())
                {
                    if (TentarCriarLinha(elemento, out var l) && l is not null)
                        linhas.Add(l);
                }

                if (linhas.Count == 0)
                {
                    _log.Aviso($"[ApiCliente] Não foi encontrando dados para a janela {dataInicio} e {dataFim}");
                    dataInicio += _tamanhoJanelaBusca;
                    continue;
                }

                _log.Info($"[ApiCliente] Inserindo marca d'agua...");
                var wm = linhas
                    .Select(linha => linha.UpdatedAtIso)
                    .Where(procura => !string.IsNullOrWhiteSpace(procura))
                    .DefaultIfEmpty(dataFim.ToString("O"))
                    .Max()!;

                foreach (var linha in linhas)
                {
                    linha.HighWaterMark = wm;
                    yield return linha;
                }

                dataInicio = NormalizarData(dataInicio);
                _log.Info($"[ApiCliente] Busca foi concluida com sucesso!");
            }
        }

        private static DateTimeOffset NormalizarData(DateTimeOffset data)
        {
            return new DateTimeOffset(data.Year, data.Month, data.Day, 0, 0, 0, data.Offset);
        }

        private static bool TentarPegarArray(JsonElement root, out JsonElement arr)
        {
            if (root.ValueKind == JsonValueKind.Array)
            {
                arr = root; return true;
            }
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("data", out var d) && d.ValueKind == JsonValueKind.Array)
                { arr = d; return true; }
                if (root.TryGetProperty("items", out var it) && it.ValueKind == JsonValueKind.Array)
                { arr = it; return true; }
            }
            arr = default; return false;
        }

        private async Task LimiteRequisicao(CancellationToken comando)
        {
            await _gate.WaitAsync(comando);

            try
            {
                long agora = DateTime.UtcNow.Ticks;
                long ultimo = Interlocked.Read(ref _lastTicks);
                TimeSpan decorrido = TimeSpan.FromTicks(agora - ultimo);

                if (decorrido < _intervalo) await Task.Delay(_intervalo - decorrido, comando);
                Interlocked.Exchange(ref _lastTicks, DateTime.UtcNow.Ticks);
            }
            finally
            {
                _gate.Release();
            }
        }

        private string ConstrutorUrl(string urlFinal, DateTimeOffset dataInicial, DateTimeOffset dataFinal)
        {
            string Fmt(DateTimeOffset d) => d.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss");

            return $"{_urlBase}{urlFinal}start_date={Fmt(dataInicial)}&end_date={Fmt(dataFinal)}";
        }

        private bool TentarCriarLinha(JsonElement elemento, out ApiLinha? linha)
        {
            try
            {
                linha = elemento.Deserialize<ApiLinha>(_opcoesSerializer);
                if (linha is null) return false;

                if (string.IsNullOrWhiteSpace(linha.UpdatedAtIso) && TentarExtrairData(elemento, out var iso))
                {
                    linha.UpdatedAtIso = iso;
                }

                return true;
            }
            catch (Exception ex)
            {
                _log.Aviso($"[ApiCliente] Falha ao desserializar item na função TentarCriarLinha(): {ex.Message}");
                linha = null;
                return false;
            }
        }
        
        private static bool TentarExtrairData(JsonElement elemento, out string isoUtc)
        {
            DateTimeOffset maiorData = DateTimeOffset.MinValue;
            bool encontrou = true;

            foreach (var nome in _candidatosData)
            {
                if (elemento.TryGetProperty(nome, out var v) && v.ValueKind == JsonValueKind.String)
                {
                    var str = v.GetString();
                    if (Utilidades.TentarPegarData(str, out var dt) && !string.IsNullOrEmpty(str))
                    {
                        dt = dt.ToUniversalTime();
                        if (dt > maiorData)
                        {
                            maiorData = dt;
                            encontrou = true;
                        } 
                    }
                }
            }

            if (encontrou)
            {
                isoUtc = maiorData.ToString("O", CultureInfo.InvariantCulture);
                return true;
            }

            isoUtc = string.Empty;
            return false;
        }
    }
}