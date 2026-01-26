
namespace TF.src.Infra.Armazenagem
{
    public interface IGuardarDados
    {
        Task<string?> ObterDados(string tabela, CancellationToken comando = default);

        Task SalvarDados(string tabela, string dados, CancellationToken comando = default);
    }
}