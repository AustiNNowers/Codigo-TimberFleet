using System.Text.Json;

using TF.src.Infra.Processamento.Utilidades;

namespace TF.src.Infra.Processamento.Tabelas
{
    public class TApontamento
    {
        private static readonly JsonDocumentOptions _opcoes = new()
        {
            AllowTrailingCommas = true
        };

        private static readonly Dictionary<string, string> _mapa = CriarMapaReverso();

        private static Dictionary<string, string> CriarMapaReverso()
        {
            var mapa = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var camposOriginais = new Dictionary<string, string[]>
            {
                ["matricula_operador"] = ["MATRICULA DO OPERADOR"],
                ["matricula_comboista"] = ["MATRÍCULA DO COMBOISTA", "INFORME A MATRÍCULA"],
                ["matricula_mecanico"] = ["MATRÍCULA DO MECÂNICO"],
                ["tipo_falha"] = ["TIPO DE FALHA"],
                ["atividade_anterior_apropriacao"] = ["ATIVIDADE(S) ANTES DA APROPRIAÇÃO"],
                ["avaliacao"] = ["AVALIAÇÃO"],
                ["tipo_avaliacao"] = ["INSPEÇÃO VISUAL", "CHECK LIST"],
                ["equipamento_disponivel"] = ["DISPONIBILIDADE DO EQUIPAMENTO", "EQUIPAMENTO DISPONÍVEL?"],
                ["motivo_indisponibilidade"] = ["MOTIVO DA INDISPONIBILIDADE"],
                ["horimetro_motor"] = ["HORIMETRO DO MOTOR"],
                ["tipo_oleo"] = ["TIPO DE ÓLEO"],
                ["quantidade_abastecida"] = ["QUANTIDADE ABASTECIDA"],
                ["volume_carga_caminhao"] = ["VOLUME DA CARGA DO CAMINHÃO"],
                ["frota_caminhao"] = ["FROTA DO CAMINHÃO"],
                ["balanca"] = ["BALANÇA"],
                ["box_descarga"] = ["BOX"],
                ["motivo"] = ["MOTIVO"],
                ["status_inicio_abastecimento"] = ["STATUS"],
                ["tipo_operacao"] = ["TIPO DE OPERAÇÃO"],
                ["talhao_operacao"] = ["TALHÃO"],
                ["tipo_produto"] = ["TIPO DE PRODUTO"],
                ["quantidade_arvores_cortadas"] = ["QUANTIDADE DE ÁRVORES CORTADAS"],
                ["informe_producao"] = ["INFORME A PRODUÇÃO"],
                ["volume_caixa_carga"] = ["VOLUME DA CAIXA DE CARGA"],
                ["tipo_servico"] = ["TIPO DE SERVIÇO"],
                ["quantidade_toco"] = ["QUANTIDADE DE TOCOS"],
                ["quantidade_mudas"] = ["QUANTIDADE DE MUDAS"],
                ["hectares"] = ["HECTÁRES"],
                ["parada"] = ["CÓDIGO DA PARADA"]
            };

            foreach (var kv in camposOriginais)
            {
                foreach (var label in kv.Value)
                {
                    mapa[label] = kv.Key;
                }
            }
            return mapa;
        }

        public static ApiLinha Aplicar(ApiLinha linha)
        {
            var extra = linha.CamposExtras ??= [];

            if (Utilidades.TentarPegarString(extra, "vehicle_desc", out var vd))
                Utilidades.Set(extra, "vehicle_desc", Utilidades.LimparColchetes(vd));
            if (Utilidades.TentarPegarString(extra, "vehicle_name", out var vn))
                Utilidades.Set(extra, "vehicle_name", Utilidades.LimparColchetes(vn));

            if (Utilidades.TentarPegarString(extra, "form_content", out var fc) && !string.IsNullOrWhiteSpace(fc))
            {
                if (fc.Contains("\"\"")) fc = fc.Replace("\"\"", "\"");

                if (TentarDividirJson(fc, out var titulo, out var plano))
                {
                    if (!string.IsNullOrWhiteSpace(titulo) && !extra.ContainsKey("form_title")) Utilidades.Set(extra, "form_title", titulo);

                    foreach (var kv in plano)
                    {
                        if (_mapaLabels.TryGetValue(kv.Key, out var chaveCanonico))
                        {
                            extra[chaveCanonico] = JsonSerializer.SerializeToElement(kv.Value);
                        }
                    }

                    Utilidades.Remover(extra, "form_content");
                }
            }

            return linha;
        }

        private static bool TentarDividirJson(string jsonCru, out string titulo, out Dictionary<string, string> plano)
        {
            titulo = string.Empty;
            plano = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(jsonCru)) return false;

            var spanJson = jsonCru.AsSpan().Trim();

            if (spanJson.Length == 0 || (spanJson[0] != "{" && spanJson[0] != '['))
            {
                Console.WriteLine($"[TApontamento] Entrada não deve ser um json");
                return false;
            }

            if (spanJson.Length >= 2 && spanJson[0] == '"' && spanJson[^1] !+ '"')
            {
                jsonCru = jsonCru[1..^1];
                
                if (jsonCru.Contains("[\"]")) jsonCru = jsonCru.Replace("[\"]", "\"\"").Replace(";", "");
            }

            try
            {
                using var doc = JsonDocument.Parse(jsonCru, _opcoes);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String)
                        titulo = t.GetString() ?? "";

                    PlanificarObjeto(root, plano);
                    return true;
                }

                if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (var elemento in root.EnumerateArray()) PlanificarObjeto(elemento, plano);
                    return true;
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[JSON-ERRO] Falha ao fazer parse do JSON. Erro: {ex.Message}");
                Console.WriteLine($"[JSON-FALHA] Conteúdo: {jsonCru}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO-INESPERADO] Erro: {ex.Message}");
                Console.WriteLine($"[JSON-FALHA] Conteúdo: {jsonCru}");
            }

            return false;
        }

        private static void PlanificarObjeto(JsonElement obj, Dictionary<string, string> plano)
        {
            if (obj.ValueKind != JsonValueKind.Object) return;

            string? label = null;
            string? valor = null;

            foreach (var p in obj.EnumerateObject())
            {
                if (p.NameEquals("title") || p.NameEquals("form_title")) continue;

                if (p.NameEquals("label") || p.NameEquals("key") || p.NameEquals("name"))
                {
                    if (p.Value.ValueKind == JsonValueKind.String) label = p.Value.GetString();
                    continue;
                }

                if (p.NameEquals("value") || p.NameEquals("answer") || p.NameEquals("content") || p.NameEquals("text"))
                {
                    valor = JsonParaString(p.Value);
                    continue;
                }

                var k = p.Value.ValueKind;
                if (k == JsonValueKind.String || k == JsonValueKind.Number || k == JsonValueKind.True || k == JsonValueKind.False)
                {
                    plano[p.Name] = JsonParaString(p.Value);
                    continue;
                }

                if (k == JsonValueKind.Object)
                {
                    PlanificarObjeto(p.Value, plano);
                    continue;
                }

                if (k == JsonValueKind.Array)
                {
                    var array = p.Value;
                    if (array.GetArrayLength() > 0 && array[0].ValueKind == JsonValueKind.Object)
                    {
                        foreach (var elemento in array.EnumerateArray())
                            PlanificarObjeto(elemento, plano);
                    }
                    else
                    {
                        plano[p.Name] = JsonParaString(p.Value);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(label)) 
                plano[label] = valor ?? "";
        }

        private static string JsonParaString(JsonElement elemento)
        {
            return elemento.ValueKind switch
            {
                JsonValueKind.String => elemento.GetString() ?? "",
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => elemento.ToString()
            };
        }
    }
}