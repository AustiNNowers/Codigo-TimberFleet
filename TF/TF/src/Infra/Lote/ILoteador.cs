namespace TF.src.Infra.Lote
{
    public interface ILoteador
    {
        IEnumerable<LoteadorPayload> Lotear(IEnumerable<Dictionary<string, object?>> envelopes, Func<Dictionary<string, object?>, DateTimeOffset?>? waterMark = null);
    }
}