using System.Globalization;
using System.Text.Json;

namespace TF.src.Infra.Armazenagem
{
    public static class GuardarDadosExcel
    {
        private static readonly SemaphoreSlim _xlsxLockDados = new(1, 1);

        public static async Task AppendXlsxEnvelopesAsync(
            string caminhoXlsx,
            string tabelaChave,
            IEnumerable<Dictionary<string, object?>> envelopes,
            CancellationToken ct)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(caminhoXlsx)!);

            var list = envelopes is IList<Dictionary<string, object?>> l ? l : envelopes.ToList();
            if (list.Count == 0) return;

            await _xlsxLockDados.WaitAsync(ct);
            try
            {
                using var wb = File.Exists(caminhoXlsx) ? new XLWorkbook(caminhoXlsx) : new XLWorkbook();

                var ws = wb.Worksheets.FirstOrDefault(w => w.Name.Equals(tabelaChave, StringComparison.OrdinalIgnoreCase))
                        ?? wb.Worksheets.Add(tabelaChave);

                var headers = ReadHeaders(ws);
                EnsureHeader(ws, headers, "utc_datetime");
                EnsureHeader(ws, headers, "tabela");
                EnsureHeader(ws, headers, "id_ident");
                EnsureHeader(ws, headers, "data_registro");

                var extraKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var env in list)
                {
                    if (!TryGetDados(env, out var dados)) continue;
                    foreach (var k in dados.Keys)
                    {
                        if (!headers.ContainsKey(k) && !extraKeys.Contains(k))
                            extraKeys.Add(k);
                    }
                }
                foreach (var k in extraKeys) EnsureHeader(ws, headers, k);

                var nextRow = (ws.LastRowUsed()?.RowNumber() ?? 1) + 1;
                foreach (var env in list)
                {
                    if (!TryGetDados(env, out var dados)) continue;

                    ws.Cell(nextRow, headers["utc_datetime"]).Value = DateTime.UtcNow;
                    ws.Cell(nextRow, headers["utc_datetime"]).Style.DateFormat.Format = "yyyy-MM-dd HH:mm:ss";

                    ws.Cell(nextRow, headers["tabela"]).Value = GetString(env, "tabela");

                    var id = GetString(dados, "id_ident");
                    if (string.IsNullOrWhiteSpace(id))
                        id = "(sem_id_ident)";
                    ws.Cell(nextRow, headers["id_ident"]).Value = id;

                    var dr = GetString(dados, "data_registro");
                    if (DateTimeOffset.TryParse(dr, CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dto))
                    {
                        ws.Cell(nextRow, headers["data_registro"]).Value = dto.UtcDateTime;
                        ws.Cell(nextRow, headers["data_registro"]).Style.DateFormat.Format = "yyyy-MM-dd HH:mm:ss";
                    }
                    else
                    {
                        ws.Cell(nextRow, headers["data_registro"]).Value = dr;
                    }

                    foreach (var (k, v) in dados)
                    {
                        if (!headers.TryGetValue(k, out var col)) continue;
                        WriteCell(ws, nextRow, col, v);
                    }

                    nextRow++;
                }

                if (ws.LastRowUsed()?.RowNumber() == 2)
                {
                    ws.Row(1).Style.Font.Bold = true;
                    ws.Columns().AdjustToContents();
                }

                wb.SaveAs(caminhoXlsx);
            }
            finally { _xlsxLockDados.Release(); }
        }

        static Dictionary<string, int> ReadHeaders(IXLWorksheet ws)
        {
            var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var headerRow = ws.Row(1);
            if (ws.LastRowUsed() is null) return dict;

            foreach (var cell in headerRow.CellsUsed())
            {
                var name = cell.GetString();
                if (!string.IsNullOrWhiteSpace(name))
                    dict[name] = cell.Address.ColumnNumber;
            }
            return dict;
        }

        static void EnsureHeader(IXLWorksheet ws, Dictionary<string, int> headers, string name)
        {
            if (headers.ContainsKey(name)) return;
            var col = headers.Count + 1;
            ws.Cell(1, col).Value = name;
            headers[name] = col;
        }

        static bool TryGetDados(Dictionary<string, object?> env, out Dictionary<string, object?> dados)
        {
            if (env.TryGetValue("dados", out var dv) && dv is Dictionary<string, object?> d)
            {
                dados = d;
                return true;
            }
            dados = default!;
            return false;
        }

        static string? GetString(Dictionary<string, object?> dict, string key)
        {
            if (!dict.TryGetValue(key, out var v) || v is null) return null;
            return v switch
            {
                string s => s,
                JsonElement je when je.ValueKind == JsonValueKind.String => je.GetString(),
                _ => v.ToString()
            };
        }

        static void WriteCell(IXLWorksheet ws, int row, int col, object? v)
        {
            if (v is null) return;

            switch (v)
            {
                case string s:
                    if (double.TryParse(s.Replace(".", "").Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var dnum))
                        ws.Cell(row, col).Value = dnum;
                    else if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture,
                                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dto))
                    {
                        ws.Cell(row, col).Value = dto.UtcDateTime;
                        ws.Cell(row, col).Style.DateFormat.Format = "yyyy-MM-dd HH:mm:ss";
                    }
                    else
                        ws.Cell(row, col).Value = s;
                    break;

                case int i: ws.Cell(row, col).Value = i; break;
                case long l: ws.Cell(row, col).Value = l; break;
                case double dd: ws.Cell(row, col).Value = dd; break;
                case float ff: ws.Cell(row, col).Value = (double)ff; break;
                case bool b: ws.Cell(row, col).Value = b ? 1 : 0; break;

                case JsonElement je:
                    if (je.ValueKind == JsonValueKind.Number && je.TryGetDouble(out var jn))
                        ws.Cell(row, col).Value = jn;
                    else if (je.ValueKind == JsonValueKind.String)
                        ws.Cell(row, col).Value = je.GetString();
                    else
                        ws.Cell(row, col).Value = je.GetRawText();
                    break;

                case Dictionary<string, object?> nested:
                    ws.Cell(row, col).Value = JsonSerializer.Serialize(nested);
                    break;

                default:
                    ws.Cell(row, col).Value = v.ToString();
                    break;
            }
        }
    }
}