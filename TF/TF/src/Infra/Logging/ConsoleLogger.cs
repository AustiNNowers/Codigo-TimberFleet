using System;
using System.IO;
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
                        var nomeArquivo = $"{agora:dd/MM/yyyy HH:mm}.log";
                        var caminhoArquivo = Path.Combine(_diretorioLogs, nomeArquivo);
                        File.AppendAllText(caminhoArquivo, $"{prefixo} {mensagem}{Environment.NewLine}{excessao}");
                    }
                    catch
                    {
                    }
                }
            }
        }
    
        public void SalvarLogs(string? texto, string titulo)
        {
            StreamWriter escritor = new StreamWriter("C:\\Users\\LUIS.BRANCO\\Logs TF\\" + titulo);

            escritor.Write(texto);

            escritor.Close();
        }
    }
}