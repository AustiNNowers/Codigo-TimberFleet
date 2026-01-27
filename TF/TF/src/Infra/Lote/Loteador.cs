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

        private readonly JsonSerializerOptions _opcoes = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = null,
            WriteIndented = false
        };

        public IEnumerable<LoteadorPayload> Lotear(
            IEnumerable<Dictionary<string, object?>> envelopes,
            Func<Dictionary<string, object?>, DateTimeOffset?>? waterMark = null)
        {
            if (envelopes is null) yield break;

            using var buffer = new MemoryStream(capacity: 64 * 1024);
            using var writer = new StreamWriter(buffer, new UTF8Encoding(false), leaveOpen: true);

            int linhas = 0;

            foreach (var envelope in envelopes)
            {
                var json = JsonSerializer.Serialize(envelope, _opcoes);
                var qtdBytesAvaliados = Encoding.UTF8.GetByteCount(json) + 1;

                if (linhas > 0 && (linhas + 1 > _maximoLinhas || buffer.Length + qtdBytesAvaliados > _maximoBytes))
                {
                    writer.Flush();
                    yield return Emit(buffer, linhas);
                    Resetar(buffer, out linhas);
                }

                writer.Write(json);
                writer.Write('\n');
                linhas++;
            }

            if (linhas > 0)
            {
                writer.Flush();
                yield return Emit(buffer, linhas);
            }
        }

        private static LoteadorPayload Emit(MemoryStream buffer, int linhas)
        {
            var raw = buffer.ToArray();
            var gzip = Gzip(raw);

            return new LoteadorPayload
            {
                BytesComprimidos = gzip,
                Quantidade = linhas,
                TamanhoBytes = gzip.LongLength
            };
        }

        private static void Resetar(MemoryStream buffer, out int linhas)
        {
            buffer.SetLength(0);
            linhas = 0;
        }

        private static byte[] Gzip(byte[] data)
        {
            using var saida = new MemoryStream();
            using (var zip = new GZipStream(saida, CompressionLevel.Optimal, leaveOpen: true))
            {
                zip.Write(data, 0, data.Length);
            }
            return saida.ToArray();
        }
    }
}