using System.Collections.Concurrent;

namespace TF.src.Infra.Armazenagem
{
    public class GuardarDadosMemoria : IGuardarDados
    {
        private readonly ConcurrentDictionary<string, string> armazenamento = new(StringComparer.OrdinalIgnoreCase);

        public Task<string?> ObterDados(string tabela, CancellationToken comando = default)
        {
            armazenamento.TryGetValue(tabela, out var valor);
            return Task.FromResult(valor);
        }

        public Task SalvarDados(string tabela, string dados, CancellationToken comando = default)
        {
            armazenamento[tabela] = dados;
            return Task.CompletedTask;
        }
    }
}