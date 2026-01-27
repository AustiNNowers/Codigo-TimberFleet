namespace TF.src.Infra.Autenticacao
{
    public interface IProvedorToken
    {
        Task<string> GerarToken(CancellationToken comando = default);
        Task RenovarToken(CancellationToken comando = default);
    }
}