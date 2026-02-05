using System.Text;

namespace TF.src.Infra.Logging
{
    public class ConsoleLogger : IConsoleLogger
    {
        private readonly object _lock = new();
        private readonly string _diretorioLogs = Directory.GetCurrentDirectory() + "/Logs/";
        public void Info(string mensagem) => Escrever("INFORMAÇÃO", ConsoleColor.DarkMagenta, ConsoleColor.White, mensagem);
        public void Aviso(string mensagem) => Escrever("AVISO", ConsoleColor.Yellow, ConsoleColor.Black, mensagem);
        public void Erro(string mensagem, Exception? excessao = null) => Escrever("ERRO", ConsoleColor.Red, ConsoleColor.White, mensagem, excessao);

        private void Escrever(string nivel, ConsoleColor corFundo, ConsoleColor corLetra, string mensagem, Exception? excessao = null)
        {
            var agora = DateTime.Now;
            var prefixo = $"[{nivel} -- {agora:dd/MM HH:mm:ss}]";

            lock (_lock)
            {
                var oldColorFonte = Console.ForegroundColor;
                var oldColorFundo = Console.BackgroundColor;
                Console.ForegroundColor = corLetra;
                Console.BackgroundColor = corFundo;
                Console.Write(prefixo);
                Console.ForegroundColor = oldColorFonte;
                Console.BackgroundColor = oldColorFundo;
                Console.WriteLine($" {mensagem}");

                if (nivel == "AVISO" || nivel == "ERRO")
                {
                    try
                    {
                        var nomeArquivo = $"{agora:dd-MM-yyyy_HH-mm}.log";
                        var caminhoArquivo = Path.Combine(_diretorioLogs, nomeArquivo);

                        var logBuilder = new StringBuilder();
                        logBuilder.Append(prefixo).Append(' ').Append(mensagem).AppendLine();
                        if (excessao != null)
                        {
                            logBuilder.AppendLine("Stack Trace:");
                            logBuilder.AppendLine(excessao.ToString());
                        }

                        File.AppendAllText(caminhoArquivo, logBuilder.ToString());                    
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"FALHA AO GRAVAR LOG EM ARQUIVO: {ex.Message}");
                    }
                }
            }
        }
    
        public void SalvarLogs(string? texto, string titulo)
        {
            if (string.IsNullOrWhiteSpace(texto)) return;

            try 
            {
                var tituloSeguro = string.Join("_", titulo.Split(Path.GetInvalidFileNameChars()));
                var caminho = Path.Combine(_diretorioLogs, "Dumps");
                
                if (!Directory.Exists(caminho)) Directory.CreateDirectory(caminho);

                var caminhoCompleto = Path.Combine(caminho, tituloSeguro + ".json");

                File.WriteAllText(caminhoCompleto, texto);
            }
            catch (Exception ex)
            {
                Aviso($"Falha ao salvar dump de log '{titulo}': {ex.Message}");
            }
        }
    }
}