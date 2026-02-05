using System.Security.Cryptography;
using TF.src.Infra.ValidacaoArquivos;

var comando = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    comando.Cancel();
    Console.WriteLine();
    Console.WriteLine("Cancelamento foi solicitado (Ctrl+C). Encerrando com segurança...");
};

string CaminhoConfiguracoesJson() =>
    Path.Combine(Directory.GetCurrentDirectory(), "configuracoes.json");

int QuantidadeParalelos()
{
    if (args.Length > 1 && int.TryParse(args[1], out var quantidade) && quantidade > 0) return quantidade;

    var quantidadeAmbiente = Environment.GetEnvironmentVariable("DOP");
    if (!string.IsNullOrWhiteSpace(quantidadeAmbiente) && int.TryParse(quantidadeAmbiente, out var qaValor) && qaValor > 0) return qaValor;

    return Environment.ProcessorCount - 1;
}

TimeSpan IntervaloLoops()
{
    if (args.Length > 2 && int.TryParse(args[2], out var secs) && secs > 0)
        return TimeSpan.FromSeconds(secs);

    var env = Environment.GetEnvironmentVariable("LOOP_SECONDS");
    if (!string.IsNullOrWhiteSpace(env) && int.TryParse(env, out var envSecs) && envSecs > 0)
        return TimeSpan.FromSeconds(envSecs);

    return TimeSpan.FromSeconds(16);
}

TimeSpan ProcessoJitter(int failureCount)
{
    var baseMs = Math.Min(60000, (int)(300 * Math.Pow(2, Math.Max(0, failureCount - 1))));
    Span<byte> b = stackalloc byte[4];
    RandomNumberGenerator.Fill(b);
    var jitterMs = Math.Abs(BitConverter.ToInt32(b)) % 7500;
    return TimeSpan.FromMilliseconds(baseMs + jitterMs);
}

var caminho = CaminhoConfiguracoesJson();
var paralelos = QuantidadeParalelos();
var intervalo = IntervaloLoops();

Console.WriteLine("==================================================");
Console.WriteLine("   TimberFleet — Vale do Tibagi");
Console.WriteLine("==================================================");
Console.WriteLine($"Config: {caminho}");
Console.WriteLine($"DOP   : {paralelos}");
Console.WriteLine("Pressione Ctrl+C para cancelar.");
Console.WriteLine();

try
{
    var scripts = await Aferidor.Construir(caminho, paralelos, comando: comando.Token);

    scripts.Logger.Info("Começando a executar os trabalhos...");

    using var timer = new PeriodicTimer(intervalo);
    var contagemFalhas = 0;
    var quantidadeLoops = 0;

    while (!comando.IsCancellationRequested)
    {
        scripts.Logger.Info($"Iniciando loop {quantidadeLoops}...");
        try
        { 
            await scripts.CanalExecucao.Executar(comando.Token);

            scripts.Logger.Info("Canal de Execução foi concluido com sucesso!");

            contagemFalhas = 0;

            await timer.WaitForNextTickAsync(comando.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Execução cancelada");
        }
        catch (Exception ex)
        {
            contagemFalhas++;

            scripts.Logger.Erro("FALHA CRÍTICA!!!");
            Console.WriteLine($"Erro que foi retornado: {ex}");

            var backoff = ProcessoJitter(contagemFalhas);
            scripts.Logger.Aviso($"Aguardando {backoff.TotalSeconds:0.0}s e tentando novamente...");

            try
            {
                await Task.Delay(backoff, comando.Token);
            }
            catch (OperationCanceledException)
            {
                scripts.Logger.Info("Cancelado durante o backoff");
                break;
            }
        }
        quantidadeLoops++;
    }

    scripts.Logger.Info($"Loop terminou, finalizado com total de {quantidadeLoops} loops!");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Execução cancelada");
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Falha crítica:");
    Console.ResetColor();
    Console.WriteLine(ex);
}
finally
{ }
