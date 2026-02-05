using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TF.src.Infra.Lote
{
    public class Loteador(int maximoLinhas, long maximoBytes) : ILoteador
    {
        private readonly int _maximoLinhas = Math.Max(1, maximoLinhas);
        private readonly long _maximoBytes = Math.Max(1024, maximoBytes);
        private static readonly byte _newLine = (byte)'\n';

        private readonly JsonSerializerOptions _opcoes = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = null,
            WriteIndented = false
        };

        public IEnumerable<LoteadorPayload> Lotear(
            IEnumerable<Dictionary<string, object?>> envelopes,
            Func<Dictionary<string, object?>, DateTime?>? waterMark = null)
        {
            if (envelopes is null) yield break;

            using var buffer = new MemoryStream(capacity: 64 * 1024);

            int linhas = 0;

            DateTime? dataMinima = null;
            DateTime? dataMaxima = null;

            foreach (var envelope in envelopes)
            {
                var json = JsonSerializer.Serialize(envelope, _opcoes);
                var qtdBytesAvaliados = Encoding.UTF8.GetByteCount(json) + 1;

                if (linhas > 0 && (linhas + 1 > _maximoLinhas || buffer.Length + qtdBytesAvaliados > _maximoBytes))
                {
                    yield return Emit(buffer, linhas, dataMinima, dataMaxima);

                    buffer.SetLength(0);
                    linhas = 0;
                    dataMinima = null;
                    dataMaxima = null;                
                }

                linhas++;
            }

            if (linhas > 0)
            {
                yield return Emit(buffer, linhas, dataMinima, dataMaxima);
            }
        }

        private static LoteadorPayload Emit(MemoryStream buffer, int linhas, DateTime? dataInicio, DateTime? dataFim)
        {
            buffer.Position = 0;

            byte[] bytesComprimidos;

            using (var saida = new MemoryStream())
            {
                using (var zip = new GZipStream(saida, CompressionLevel.Optimal, leaveOpen: true))
                {
                    buffer.CopyTo(zip);
                }
                bytesComprimidos = saida.ToArray();
            }

            return new LoteadorPayload
            {
                BytesComprimidos = bytesComprimidos,
                Quantidade = linhas,
                TamanhoBytes = bytesComprimidos.LongLength,
                DataInicio = dataInicio?.ToUniversalTime(),
                DataFim = dataFim?.ToUniversalTime()
            };
        }

        public IEnumerable<LoteadorPayload> Lotear(IEnumerable<Dictionary<string, object?>> envelopes, Func<Dictionary<string, object?>, DateTimeOffset?>? waterMark = null)
        {
            throw new NotImplementedException();
        }
    }
}