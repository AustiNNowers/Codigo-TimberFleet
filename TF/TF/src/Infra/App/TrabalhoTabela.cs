using System.Globalization;

using TF.src.Infra.Armazenagem;
using TF.src.Infra.Coletor;
using TF.src.Infra.Configuracoes;
using TF.src.Infra.Logging;
using TF.src.Infra.Lote;
using TF.src.Infra.Modelo;
using TF.src.Infra.Processamento;
using TF.src.Infra.Processamento.Payloads;
using TF.src.Infra.Upload;

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
            if (string.IsNullOrWhiteSpace(cursorIso))
            {
                cursorIso = _config.UltimaAtualizacao;
                _log.Info($"[Trabalho atual: {_tabelaChave}] Cursor ausente no IGuardarDados | Utilizando o config.UltimaAtualizacao = '{cursorIso}'.");
            }

            if (!Utilidades.TentarPegarData(cursorIso, out var data))
            {
                data = DateTimeOffset.UtcNow.AddDays(-3);
                _log.Aviso($"[Trabalho Atual: {_tabelaChave}] Cursor inválido ('{cursorIso}') | Utilizando um fallback {data:O}.");
            }

            _log.Info($"[Trabalho Atual: {_tabelaChave}] Janela de busca de dados: {data} > {DateTimeOffset.UtcNow}");

            var bufferLinhas = new List<ApiLinha>(_bufferLinhasPre * 2);
            string? maiorIsoVista = cursorIso;

            int totalLinhas = 0, totalLotes = 0;

            _log.Info($"[Trabalho Atual: {_tabelaChave}] Iniciando ColetaDados...");
            await foreach (var linha in _coletor.ColetarDados(
                _config.UrlFinal,
                data,
                comando
            ))
            {
                comando.ThrowIfCancellationRequested();

                if (!string.IsNullOrWhiteSpace(linha.UpdatedAtIso) &&
                    Utilidades.TentarPegarData(linha.UpdatedAtIso, out var luai))
                {
                    if (Utilidades.TentarPegarData(maiorIsoVista, out var dataMaior))
                    {
                        if (luai > dataMaior) maiorIsoVista = luai.ToUniversalTime().ToString("O");
                    }
                    else
                    {
                        maiorIsoVista = luai.ToUniversalTime().ToString("O");
                    }
                }

                bufferLinhas.Add(linha);
                totalLinhas++;
                
                if (bufferLinhas.Count >= _bufferLinhasPre)
                {
                    _log.Info($"[Trabalho Atual: {_tabelaChave}] | Processando a linha...");
                    await ProcessarBlocoAsync(bufferLinhas, totalLotes, maiorIsoVista, comando);
                    bufferLinhas.Clear();
                }
            }

            if (bufferLinhas.Count > 0)
            {
                await ProcessarBlocoAsync(bufferLinhas, totalLotes, maiorIsoVista, comando);
                bufferLinhas.Clear();
            }

            _log.Info($"[Trabalho Atual: {_tabelaChave}] Concluído. Linhas totais: {totalLinhas} | Lotes totais: {totalLotes} | Nova Data Cursor: {maiorIsoVista ?? "Sem alteração"}");
        }

        private async Task ProcessarBlocoAsync(
            List<ApiLinha> bloco,
            int totalLotes,
            string? maiorIsoVista,
            CancellationToken comando
        )
        {
            _log.Info($"[Trabalho Atual: {_tabelaChave}] | Tratando a linha");
            var linhasTransformadas = _transformar.Transformar(_tabelaChave, bloco);

            _log.Info($"[Trabalho Atual: {_tabelaChave}] | Envelopando a linha");
            var envelopes = linhasTransformadas.Select(linhas =>
            {
                return PayloadConstrutor.Construir(_config.UrlFinal, linhas, out var env) ? env : null;
            }).Where(dados => dados is not null)!.Cast<Dictionary<string, object?>>();

            _log.Info($"[Trabalho Atual: {_tabelaChave}] | Loteando...");
            foreach (var lote in _loteador.Lotear(envelopes, WaterMark))
            {
                await _uploader.UploadPhp(lote, comando);
                totalLotes++;

                if (!string.IsNullOrWhiteSpace(maiorIsoVista)) await _guardarDados.SalvarDados(_tabelaChave, maiorIsoVista, comando);
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