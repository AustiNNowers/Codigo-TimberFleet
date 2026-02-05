using System.Net;

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
                    var resposta = await http.SendAsync(requisicao, HttpCompletionOption.ResponseHeadersRead, cts.Token);

                    if ((int)resposta.StatusCode >= 500 || resposta.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        var body = await resposta.Content.ReadAsStringAsync(cts.Token);
                        logDebug?.Invoke(
                            $"[PoliticaRetentativa] {(int)resposta.StatusCode} {resposta.ReasonPhrase} " +
                            $"| Headers: {resposta.Headers} " +
                            $"| Body: {(body.Length > 800 ? body[..800] + "…" : body)}"
                        );
                        resposta.Dispose();
                        await Task.Delay(CalcularAtraso(atraso, tentativa), comando);
                        continue;
                    }

                    return resposta;
                }
                catch (Exception ex) when (ex is TaskCanceledException || ex is HttpRequestException)
                {
                    if (tentativa == tentativasMaxima) throw;

                    logDebug?.Invoke($"[PoliticaRetentativa] Erro transiente: {ex.Message}. Tentando novamente...");
                    await Task.Delay(CalcularAtraso(atraso, tentativa), comando);
                }
            }

            throw new TimeoutException($"Tentativas excedidas!");
        }

        private static TimeSpan CalcularAtraso(TimeSpan baseDelay, int tentativa)
        {
            var fator = Math.Pow(2, tentativa - 1);
            var jitter = Random.Shared.Next(0, 250);

            var delay = TimeSpan.FromMilliseconds((baseDelay.TotalMilliseconds * fator) + jitter);

            var max = TimeSpan.FromSeconds(30);
            return delay > max ? max : delay;
        }
    }
}