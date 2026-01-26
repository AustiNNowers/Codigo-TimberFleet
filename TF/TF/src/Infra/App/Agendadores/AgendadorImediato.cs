namespace TF.src.Infra.App.Agendadores
{
    public class AgendadorImediato(int paralelos) : IAgendadorTabela
    {
        private readonly int _paralelos = Math.Max(1, paralelos);

        public async Task Executar(IEnumerable<Func<CancellationToken, Task>> trabalhos, CancellationToken comando = default)
        {
            var lista = trabalhos?.ToList() ?? [];
            if (lista.Count == 0) return;

            int index = -1;

            var trabalhadores = new Task[_paralelos];
            for (int t = 0; t < _paralelos; t++)
            {
                trabalhadores[t] = Task.Run(async () =>
                {
                    while (true)
                    {
                        comando.ThrowIfCancellationRequested();

                        int i = Interlocked.Increment(ref index);
                        if (i >= lista.Count) break;

                        var trabalho = lista[i];

                        try
                        {
                            await trabalho(comando);
                        }
                        catch
                        {

                        }
                    }
                }, comando);
            }

            await Task.WhenAll(trabalhadores);
        }
    }
}