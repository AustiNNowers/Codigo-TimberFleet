using TF.src.Infra.Modelo;

namespace TF.src.Infra.Processamento.Payloads
{
    public static class PayloadConstrutor
    {
        public static bool Construir(string endUrl, ApiLinha linha, out Dictionary<string, object?> envelope)
        {
            envelope = default!;
            return endUrl.ToLowerInvariant() switch
            {
                "counters?" => ContadoresPayload.TryBuild(linha, out envelope),
                "forms?" => FormularioPayload.TryBuild(linha, out envelope),
                "notes?" => ApontamentosPayload.TryBuild(linha, out envelope),
                "points?" => LocalizacaoPayload.TryBuild(linha, out envelope),
                "new_production?" => ProducaoPayload.TryBuild(linha, out envelope),
                "new_production_products?" => ProdutoPayload.TryBuild(linha, out envelope),
                _ => false,
            };
        }
    }
}
