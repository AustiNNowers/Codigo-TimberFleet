using System.Globalization;

using TF.src.Infra.Processamento.Utilidades;

namespace TF.src.Infra.App
{
    public class TrabalhoTabela(
        string tabelaChave, TabelaMeta config, IGuardarDados guardarDados, IColetorDados coletor, ITransformarDados transformar, ILoteador loteador,
        IUploader uploader, IConsoleLogger log, int bufferLinhasPre = 3000
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
            DateTime cursorData;

            if (string.IsNullOrWhiteSpace(cursorIso) || !Utilidades.TentarPegarData(cursorIso, out cursorData))
            {
                if (!string.IsNullOrWhiteSpace(_config.UltimaAtualizacao) && Utilidades.TentarPegarData(_config.UltimaAtualizacao, out var configData))
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

            _log.Info($"[Trabalho Atual: {_tabelaChave}] Janela de busca de dados: {data} > {DateTime.Now}");

            var bufferLinhas = new List<ApiLinha>(_bufferLinhasPre);

            DateTime maiorIsoVista = cursorData;

            int totalLinhas = 0, totalLotes = 0;

            _log.Info($"[Trabalho Atual: {_tabelaChave}] Iniciando ColetaDados...");
            
            await foreach (var linha in _coletor.ColetarDados(
                _config.UrlFinal,
                cursorData,
                comando
            ))
            {
                comando.ThrowIfCancellationRequested();

                if (!string.IsNullOrWhiteSpace(linha.UpdatedAtIso) && Utilidades.TentarPegarData(linha.UpdatedAtIso, out var luai))
                    if (luai > maiorIsoVista) maiorIsoVista = luai;

                bufferLinhas.Add(linha);
                totalLinhas++;
                
                if (bufferLinhas.Count >= _bufferLinhasPre)
                {
                    _log.Info($"[Trabalho Atual: {_tabelaChave}] | Processando a linha...");
                    var lotesProcessados = await ProcessarBlocoAsync(bufferLinhas, maiorIsoVista, comando);
                    totalLotes += lotesProcessados;

                    await _guardarDados.SalvarDados(_tabelaChave, maiorIsoVista.ToString("O"), comando);
                    bufferLinhas.Clear();
                }
            }

            if (bufferLinhas.Count > 0)
            {
                var lotesProcessados = await ProcessarBlocoAsync(bufferLinhas, maiorIsoVista, comando);
                totalLotes += lotesProcessados;

                await _guardarDados.SalvarDados(_tabelaChave, maiorIsoVista.ToString("O"), comando);
                bufferLinhas.Clear();
            }

            _log.Info($"[Trabalho Atual: {_tabelaChave}] Concluído. Linhas totais: {totalLinhas} | Lotes totais: {totalLotes} | Nova Data Cursor: {maiorIsoVista ?? "Sem alteração"}");
        }

        private async Task ProcessarBlocoAsync(
            List<ApiLinha> bloco,
            DateTime? maiorIsoVista,
            CancellationToken comando
        )
        {
            _log.Info($"[Trabalho Atual: {_tabelaChave}] | Transformando {bloco.Count} linhas...");
            var linhasTransformadas = _transformar.Transformar(_tabelaChave, bloco);

            _log.Info($"[Trabalho Atual: {_tabelaChave}] | Envelopando a linha");
            var envelopes = new List<Dictionary<string, object?>>(bloco.Count);
            foreach (var linha in linhasTransformadas)
            {
                if (PayloadConstrutor.Construir(_config.UrlFinal, linha, out var env))
                {
                    envelopes.Add(env!);
                }
            }

            int lotesEnviados = 0;

            _log.Info($"[Trabalho Atual: {_tabelaChave}] | Loteando e Enviando...");
            foreach (var lote in _loteador.Lotear(envelopes, WaterMark))
            {
                await _uploader.UploadPhp(lote, comando);
                lotesEnviados++;
            }

            return lotesEnviados;
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