using System.Globalization;
using System.Threading.Channels;
using TF.src.Infra.Processamento.Payloads;
using TF.src.Infra.Logging;
using TF.src.Infra.Upload;
using TF.src.Infra.Processamento;
using TF.src.Infra.Coletor;
using TF.src.Infra.Armazenagem;
using TF.src.Infra.Configuracoes;
using TF.src.Infra.Lote;
using TF.src.Infra.Modelo;

namespace TF.src.Infra.App
{
    public class TrabalhoTabela(
        string tabelaChave, TabelaMeta config, IGuardarDados guardarDados, IColetorDados coletor, ITransformarDados transformar, ILoteador loteador,
        IUploader uploader, IConsoleLogger log, int bufferLinhasPre = 3000, CancellationToken comando = default
        )
    {
        private readonly string _tabelaChave = tabelaChave;
        private readonly TabelaMeta _config = config;

        private readonly IColetorDados _coletor = coletor;
        private readonly ITransformarDados _transformar = transformar;
        private readonly ILoteador _loteador = loteador;
        private readonly IUploader _uploader = uploader;
        private readonly IConsoleLogger _log = log;
        private readonly IGuardarDados _guardarDados = guardarDados;
        private readonly int _bufferLinhasPre = bufferLinhasPre;

        public async Task Executar(CancellationToken comando = default)
        {
            _log.Info($"[Trabalho Atual: {_tabelaChave}] Iniciando...");

            string? cursorIso = await _guardarDados.ObterDados(_tabelaChave, comando);
            DateTimeOffset cursorData;

            if (string.IsNullOrWhiteSpace(cursorIso) || !Utilidades.TentarPegarData(cursorIso, out cursorData))
            {
                if (Utilidades.TentarPegarData(_config.UltimaAtualizacao.ToString(), out var configData))
                {
                    cursorData = configData;
                    _log.Info($"[Trabalho atual: {_tabelaChave}] Usando cursor da config: '{cursorData:O}'.");
                }
                else
                {
                    cursorData = DateTime.UtcNow.AddDays(-3);
                    _log.Aviso($"[Trabalho: {_tabelaChave}] Sem cursor válido. Usando fallback: {cursorData:O}.");
                }
            }

            var opcoesCanal = new BoundedChannelOptions(5)
            {
                SingleReader = true,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.Wait
            };

            var canal = Channel.CreateBounded<(List<ApiLinha> Dados, DateTime? CursorFIFO)>(opcoesCanal);

            var tarefaProduzente = ProduzirDados(canal.Writer, cursorData, comando);
            var tarefaComsumidora = ConsumirDados(canal.Reader, comando);

            await Task.WhenAll(tarefaComsumidora, tarefaProduzente);

            _log.Info($"[Trabalho Atual: {_tabelaChave}] Canais de Execução da Tabela foi finalizado com sucesso!");
        }

        private async Task ProduzirDados(ChannelWriter<(List<ApiLinha>, DateTime?)> escritor, DateTimeOffset cursorInicial, CancellationToken token)
        {
            try
            {
                var buffer = new List<ApiLinha>(_bufferLinhasPre);
                DateTime? maiorData = cursorInicial.UtcDateTime;
                bool teveAtualizacao = false;

                int totalLinhas = 0, totalLotesDespachados = 0;

                _log.Info($"[Trabalho Atual: {_tabelaChave}] Iniciando ColetaDados...");
                
                await foreach (var linha in _coletor.ColetarDados(
                    _config.UrlFinal,
                    cursorInicial,
                    comando
                ))
                {
                    comando.ThrowIfCancellationRequested();

                    if (!string.IsNullOrWhiteSpace(linha.UpdatedAtIso) && Utilidades.TentarPegarData(linha.UpdatedAtIso, out var luai))
                        if (luai > maiorData)
                        {
                            maiorData = luai.UtcDateTime;
                            teveAtualizacao = true;
                        }

                    buffer.Add(linha);
                    totalLinhas++;
                    
                    if (buffer.Count >= _bufferLinhasPre)
                    {
                        _log.Info($"[Trabalho Atual: {_tabelaChave}] | Buffer enchou, despachando para o canal...");
                        var pacote = new List<ApiLinha>(buffer);

                        await escritor.WriteAsync((pacote, teveAtualizacao ? maiorData : null), comando);
                        totalLotesDespachados++;
                        buffer.Clear();
                    }
                }

                if (buffer.Count > 0)
                {
                    await escritor.WriteAsync((buffer, teveAtualizacao ? maiorData : null), comando);
                }

                escritor.Complete();
                _log.Info($"[Trabalho Atual: {_tabelaChave}] Concluído. Linhas totais: {totalLinhas} | Lotes totais: {totalLotesDespachados} | Nova Data Cursor: {(maiorData == null ? "Sem alteração" : maiorData)}");
            }
            catch (Exception ex)
            {
                _log.Erro($"[Produtor {_tabelaChave}] Falha no download: {ex.Message}");
                escritor.Complete(ex);
                throw;
            }
        }

        private async Task ConsumirDados(
            ChannelReader<(List<ApiLinha> Dados, DateTime? cursorFIFO)> leitor,
            CancellationToken comando
        )
        {
            int lotesProcessados = 0;

            await foreach(var (linhas, cursorParaSalvar) in leitor.ReadAllAsync(comando))
            {
                try
                {
                    _log.Info($"[Consumidor {_tabelaChave}] Processando {linhas.Count} linhas...");

                    var transformadas = _transformar.Transformar(_tabelaChave, linhas);
                    var envelopes = new List<Dictionary<string, object?>>(linhas.Count);
                    foreach (var linha in transformadas)
                    {
                        if (PayloadConstrutor.Construir(_config.UrlFinal, linha, out var env))
                            envelopes.Add(env!);
                    }

                    foreach (var subLote in _loteador.Lotear(envelopes, WaterMark))
                    {
                        await _uploader.UploadPhp(subLote, comando);
                        lotesProcessados++;
                    }

                    if (cursorParaSalvar.HasValue)
                    {
                        await _guardarDados.SalvarDados(_tabelaChave, cursorParaSalvar.Value.ToUniversalTime().ToString("O"), comando);
                    }
                }
                catch (Exception ex)
                {
                    _log.Erro($"[Consumidor {_tabelaChave}] Falha no processamento/upload dos dados: {ex.Message}");
                    throw;
                }
            }
        }

        private static DateTimeOffset? WaterMark(Dictionary<string, object?> envelope)
        {
            if (!envelope.TryGetValue("dados", out var dv) || dv is not Dictionary<string, object?> dados) return null;

            if (!dados.TryGetValue("data_registro", out var v) || v is not string str) return null;

            if (DateTimeOffset.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt)) return dt;

            return null;
        }
    }
}