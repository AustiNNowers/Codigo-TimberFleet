namespace TF.src.Infra.App.Agendadores
{
    public class AgendadorImediato(int paralelos, IConsoleLogger log = null) : IAgendadorTabela
    {
        private readonly int _paralelos = Math.Max(1, paralelos);
        private readonly IConsoleLogger _log = log;

        public async Task Executar(IEnumerable<Func<CancellationToken, Task>> trabalhos, CancellationToken comando = default)
        {
            var listaTrabalho = trabalhos?? Enumerable.Empty<Func<CancellationToken, Task>>();

            await Parallel.ForEachAsync(
                listaTrabalho,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = _paralelos,
                    CancellationToken = comando
                },
                async (trabalho, token) =>
                {
                    try
                    {
                        await trabalho(token);
                    }
                    catch (Exception ex)
                    {
                        _log.Erro($"[Agendador] Falha não tratada na execução de uma tabela: {ex.Message}");
                    }
                }
            );
        }
    }
}