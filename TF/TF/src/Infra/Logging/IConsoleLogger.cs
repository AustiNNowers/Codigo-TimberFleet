namespace TF.src.Infra.Logging
{
    public interface IConsoleLogger
    {
        void Info(string mensagem);
        void Aviso(string mensagem);
        void Erro(string mensagem, Exception? excessao = null);

        void SalvarLogs(string? texto, string titulo);
    }
}