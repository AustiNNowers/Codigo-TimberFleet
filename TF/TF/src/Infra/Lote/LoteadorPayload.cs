namespace TF.src.Infra.Lote
{
    public class LoteadorPayload
    {
        public required byte[] BytesComprimidos { get; init; }
        public required int Quantidade { get; init; }
        public required long TamanhoBytes { get; init; }
        public string? DataInicio { get; init; }
        public string? DataFim { get; init; }
        public override string ToString()
        {
            return $"[Quantidade: {Quantidade} linhas, Tamanho: {TamanhoBytes} bytes]";
        }
    }
}