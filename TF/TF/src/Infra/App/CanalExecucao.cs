using TF.src.Infra.App.Agendadores;
using TF.src.Infra.Armazenagem;
using TF.src.Infra.Coletor;
using TF.src.Infra.Configuracoes;
using TF.src.Infra.Logging;
using TF.src.Infra.Lote;
using TF.src.Infra.Processamento;
using TF.src.Infra.Upload;

namespace TF.src.Infra.App
{
    public class CanalExecucao(
        IConfigProvider config, IGuardarDados guardarDados, IColetorDados coletor, ITransformarDados transformar, ILoteador loteador,
        IUploader uploader, IConsoleLogger log, IAgendadorTabela agendador
        )
    {
        private readonly IConfigProvider _config = config;
        private readonly IGuardarDados _guardarDados = guardarDados;

        private readonly IColetorDados _coletor = coletor;
        private readonly ITransformarDados _transformar = transformar;
        private readonly ILoteador _loteador = loteador;
        private readonly IUploader _uploader = uploader;
        private readonly IConsoleLogger _log = log;
        private readonly IAgendadorTabela _agendador = agendador;
        
        public async Task Executar(CancellationToken comando = default)
        {
            var configuracao = await _config.CarregarConfiguracao(comando);
            if (configuracao.Tabelas is null || configuracao.Tabelas.Count == 0)
            {
                _log.Aviso("[CanalExecucao] Nenhuma tabela foi retornada do ConfigJson.cs");
                return;
            }

            _log.Info($"[CanalExecucao] Iniciando {configuracao.Tabelas.Count} tabelas(s)...");

            var trabalhos = new List<Func<CancellationToken, Task>>(configuracao.Tabelas.Count);
            foreach (var kv in configuracao.Tabelas)
            {
                var tabelaChave = kv.Key;
                var tabelaMeta = kv.Value;

                if (tabelaMeta.TabelaAtiva is false)
                {
                    _log.Info($"[CanalExecucao]: {tabelaChave} Tabela desabilitada, seguindo para a proxima...");
                    continue;
                }

                trabalhos.Add(async (comando) =>
                {
                    try
                    {
                        var trabalho = new TrabalhoTabela(
                            tabelaChave,
                            tabelaMeta,
                            _guardarDados,
                            _coletor,
                            _transformar,
                            _loteador,
                            _uploader,
                            _log,
                            3000);
                        await trabalho.Executar(comando);
                    }
                    catch (Exception ex)
                    {
                        _log.Erro($"[CanalExecucao]: {tabelaChave} | Falhou ao tentar criar TrabalhoTabela: {ex.Message}");
                    }
                });
            }

            await _agendador.Executar(trabalhos, comando);
            _log.Info("[CanalExecucao] Todas as tabelas foram processadas");
        }
    }
}