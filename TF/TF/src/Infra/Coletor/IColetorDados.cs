namespace TF.src.Infra.Coletor
{
    public interface IColetorDados
    {
        IAsyncEnumerable<ApiLinha> ColetarDados(
            string urlFinal,
            DateTimeOffset dataAtual,
            CancellationToken comando = default
        );
    }
}