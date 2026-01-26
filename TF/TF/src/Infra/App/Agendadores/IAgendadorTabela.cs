namespace TF.src.Infra.App.Agendadores
{
    public interface IAgendadorTabela
    {
        Task Executar(IEnumerable<Func<CancellationToken, Task>> trabalhos, CancellationToken comando = default);
    }
}