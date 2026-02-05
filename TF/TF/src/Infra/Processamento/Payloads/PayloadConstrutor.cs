using TF.src.Infra.Modelo;

namespace TF.src.Infra.Processamento.Payloads
{
    public static class PayloadConstrutor
    {
        public static bool Construir(string endUrl, ApiLinha linha, out Dictionary<string, object?> envelope)
        {
            envelope = default!;
            if (string.IsNullOrEmpty(endUrl)) return false;

            if (TerminaCom(endUrl, "counters?") || TerminaCom(endUrl, "counters"))
                return ContadoresPayload.TryBuild(linha, out envelope);

            if (TerminaCom(endUrl, "forms?") || TerminaCom(endUrl, "forms"))
                return FormularioPayload.TryBuild(linha, out envelope);

            if (TerminaCom(endUrl, "notes?") || TerminaCom(endUrl, "notes"))
                return ApontamentosPayload.TryBuild(linha, out envelope);

            if (TerminaCom(endUrl, "points?") || TerminaCom(endUrl, "points"))
                return LocalizacaoPayload.TryBuild(linha, out envelope);
            
            if (TerminaCom(endUrl, "new_production?") || TerminaCom(endUrl, "new_production"))
                return ProducaoPayload.TryBuild(linha, out envelope);

            if (TerminaCom(endUrl, "new_production_products?") || TerminaCom(endUrl, "new_production_products"))
                return ProdutoPayload.TryBuild(linha, out envelope); 

            return false;
        }

        private static bool TerminaCom(string url, string sulfixo)
        {
            return url.EndsWith(sulfixo, StringComparison.OrdinalIgnoreCase);
        }
    }
}