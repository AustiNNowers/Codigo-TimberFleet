namespace TF.src.Infra.Lote
{
    public class LoteadorPayload
    {
        public required byte[] BytesComprimidos { get; init; }
        public required int Quantidade { get; init; }
        public required long TamanhoBytes { get; init; }
        public DateTime? DataInicio { get; init; }
        public DateTime? DataFim { get; init; }
        public override string ToString()
        {
            return $"[Quantidade: {Quantidade} linhas, Tamanho: {TamanhoBytes} bytes]";
        }
    }
}