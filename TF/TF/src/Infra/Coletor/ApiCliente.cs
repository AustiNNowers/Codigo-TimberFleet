using System.Globalization;
using System.Net;
using System.Text.Json;

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
        TimeSpan? tamanhoJanelaBusca = null,
        TimeSpan? margemVerificacao = null
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

        private readonly TimeSpan _tamanhoJanelaBuscaHMS = TimeSpan.FromHours(23) + TimeSpan.FromMinutes(59) + TimeSpan.FromSeconds(59);
        private readonly DateTimeOffset _dataAtual = DateTimeOffset.UtcNow;
        private readonly HttpClient _http = http;
        private readonly IProvedorToken _provedorToken = provedorToken;
        private readonly IConsoleLogger _log = log;
        private readonly TimeSpan _intervalo = intervalo;
        private readonly TimeSpan _tamanhoJanelaBusca = tamanhoJanelaBusca > TimeSpan.FromDays(3) ? TimeSpan.FromDays(3) : tamanhoJanelaBusca ?? TimeSpan.FromDays(1);
        private readonly TimeSpan _margemVerificacaoD = margemVerificacao ?? TimeSpan.FromDays(3);
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
            DateTimeOffset dataInicio = NormalizarData(dataBusca) - _margemVerificacaoD;
            _log.Info($"[ApiCliente] DataInicio: {dataInicio} | Iniciando função...");

            while (dataInicio < _dataAtual)
            {
                _log.Info($"[ApiCliente] Iniciando o tempo de espera de Requisição");
                await LimiteRequisicao(comando);

                DateTimeOffset dataFim = (dataInicio + _tamanhoJanelaBusca) > _dataAtual ? _dataAtual : (dataInicio + (_tamanhoJanelaBusca + _tamanhoJanelaBuscaHMS));

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
                var resposta = await PoliticaRetentativa.ExecutarNovamenteRequisicao(
                    _http, criarRequisicao, tentativasMaxima: 5, atrasoBase: TimeSpan.FromMilliseconds(500), timeoutPorTentativa: TimeSpan.FromSeconds(300), comando: comando, logDebug: s => _log.Aviso(s));


                if (resposta.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _log.Aviso("[ApiCliente] 401 Não autorizado | Renovando token e tentando novamente...");
                    resposta.Dispose();
                    token = await _provedorToken.GerarToken(comando);

                    resposta = await PoliticaRetentativa.ExecutarNovamenteRequisicao(
                        _http, criarRequisicao, tentativasMaxima: 5, atrasoBase: TimeSpan.FromMilliseconds(500), timeoutPorTentativa: TimeSpan.FromSeconds(300), comando: comando, logDebug: s => _log.Aviso(s));
                }

                var body = resposta.Content.ReadAsStringAsync(comando).ToString();

                _log.SalvarLogs(body, "ApiCliente_" + urlFinal.Replace("?", "") + "_" + DateTime.UtcNow.ToString().Replace("/", "-").Replace(":", "-"));

                _log.Info($"[ApiCliente] Conexão foi feita com sucesso! Codigo de retorno: {resposta.StatusCode} | Resposta de Retorno: {resposta.Content}");
                resposta.EnsureSuccessStatusCode();

                _log.Info($"[ApiCliente] Tratando o json...");
                var json = await resposta.Content.ReadAsStringAsync(comando);

                using var doc = JsonDocument.Parse(json, _opcoesDocument);

                if (!TentarPegarArray(doc.RootElement, out var arr))
                {
                    _log.Aviso("[ApiCliente] Resposta vinda não é um aray e nem contém 'data'/'items'");
                    _log.Info($"[ApiCliente] A informação veio dessa maneira: {json}");
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
            DateTimeOffset dt = data.AddHours(-data.Hour).AddMinutes(-data.Minute).AddSeconds(-data.Second);

            return dt + TimeSpan.FromDays(3);
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
                var agora = DateTime.UtcNow.Ticks;
                var tempoDecorrido = TimeSpan.FromTicks(agora - Interlocked.Read(ref _lastTicks));
                if (tempoDecorrido < _intervalo) await Task.Delay(_intervalo - tempoDecorrido, comando);
                Interlocked.Exchange(ref _lastTicks, DateTime.UtcNow.Ticks);
            }
            finally
            {
                _gate.Release();
            }
        }

        private string ConstrutorUrl(string urlFinal, DateTimeOffset dataInicial, DateTimeOffset dataFinal)
        {
            static string formatado(DateTimeOffset data) => data.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss");

            return $"{_urlBase}{urlFinal}start_date={formatado(dataInicial)}&end_date={formatado(dataFinal)}";
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
            var candidatos = new[]
            {
                "updated_at", "updatedAt", "equip_date", "final_date", "start_date", "date", "data"
            };

            DateTimeOffset? saidaData = null;
            foreach (var nome in candidatos)
            {
                if (elemento.TryGetProperty(nome, out var v) && v.ValueKind == JsonValueKind.String)
                {
                    var str = v.GetString();
                    if (Utilidades.TentarPegarData(str, out var dt))
                    {
                        dt = dt.ToUniversalTime();
                        if (saidaData is null || dt > saidaData.Value) saidaData = dt;
                    }
                }
            }

            if (saidaData is not null)
            {
                isoUtc = saidaData.Value.ToString("O", CultureInfo.InvariantCulture);
                return true;
            }

            isoUtc = string.Empty;
            return false;
        }
    }
}