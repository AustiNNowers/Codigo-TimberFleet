using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using System.Diagnostics;

namespace TF.src.Infra.Politica
{
    public static class PoliticaRetentativa
    {
        public static async Task<HttpResponseMessage> ExecutarNovamenteRequisicao(
            HttpClient http,
            Func<HttpRequestMessage> criarRequisicao,
            int tentativasMaxima = 5,
            TimeSpan? atrasoBase = null,
            TimeSpan? timeoutPorTentativa = null,
            CancellationToken comando = default,
            Action<string>? logDebug = null
        )
        {
            var atraso = atrasoBase ?? TimeSpan.FromMilliseconds(500);
            var tentativaPorChamada = timeoutPorTentativa ?? TimeSpan.FromSeconds(20);
            var rnd = new Random();

            for (int tentativa = 1; tentativa <= tentativasMaxima; tentativa++)
            {
                comando.ThrowIfCancellationRequested();

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(comando);
                cts.CancelAfter(tentativaPorChamada);

                using var requisicao = criarRequisicao();

                try
                {
                    var sw = Stopwatch.StartNew();
                    var resposta = await http.SendAsync(requisicao, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                    sw.Stop();

                    var tipoResposta = resposta.GetType();

                    if ((int)resposta.StatusCode >= 500 || resposta.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        var body = await resposta.Content.ReadAsStringAsync();
                        logDebug?.Invoke(
                            $"[PoliticaRetentativa] {(int)resposta.StatusCode} {resposta.ReasonPhrase} " +
                            $"| Headers: {resposta.Headers} " +
                            $"| Body: {(body.Length > 800 ? body[..800] + "…" : body)}"
                        );
                        resposta.Dispose();
                        await Task.Delay(CalcularAtraso(atraso, tentativa, rnd), comando);
                        continue;
                    }

                    return resposta;
                }
                catch (TaskCanceledException ex) when (!comando.IsCancellationRequested)
                {
                    logDebug.Invoke($"[Politica Retentativa] Tarefa foi cancelada!!! - Mensagem de Cancelamento: {ex.Message}");
                    await Task.Delay(CalcularAtraso(atraso, tentativa, rnd), comando);
                    continue;
                }
                catch (HttpRequestException ex)
                {
                    logDebug.Invoke($"[Politica Retentativa] Houve um erro de requisição: {ex.Message}");
                    await Task.Delay(CalcularAtraso(atraso, tentativa, rnd), comando);
                    continue;
                }
            }

            throw new TimeoutException($"Tentativas excedidas!");
        }

        private static TimeSpan CalcularAtraso(TimeSpan baseDelay, int tentativa, Random rnd)
        {
            var fator = Math.Pow(2, tentativa - 1);
            var jitter = TimeSpan.FromMilliseconds(rnd.Next(0, 250));
            var delay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * fator) + jitter;

            var max = TimeSpan.FromSeconds(10);
            return delay > max ? max : delay;
        }

        private static bool StatusHttp(HttpStatusCode status)
        {
            return (int)status == 429 || ((int)status >= 500 && (int)status <= 599);
        }

        private static bool VerificacaoTemporario(Exception ex)
        {
            return ex is HttpRequestException
                || ex is TaskCanceledException
                || ex is IOException
                || ex.InnerException is SocketException;
        }

        private static TimeSpan Jitter(TimeSpan atrasoBase, int tentativa)
        {
            var exponecial = TimeSpan.FromMilliseconds(atrasoBase.TotalMilliseconds * Math.Pow(2, tentativa - 1));
            if (exponecial > TimeSpan.FromSeconds(15)) exponecial = TimeSpan.FromSeconds(15);

            var jitterMili = RandomJitter(0, 250);
            return exponecial + TimeSpan.FromMilliseconds(jitterMili);
        }

        private static int RandomJitter(int minInclusive, int maxInclusive)
        {
            Span<byte> b = stackalloc byte[4];
            RandomNumberGenerator.Fill(b);
            int bitCru = BitConverter.ToInt32(b);

            bitCru = Math.Abs(bitCru);
            return minInclusive + (bitCru % (maxInclusive - minInclusive + 1));
        }
    }
}