using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

using static TF.src.Infra.Processamento.Utilidades;
using TF.src.Infra.Modelo;

namespace TF.src.Infra.Processamento.Tabelas
{
    public class TApontamento
    {
        private static readonly JsonDocumentOptions _opcoes = new()
        {
            AllowTrailingCommas = true
        };

        private static readonly Dictionary<string, string[]> Campos = new(StringComparer.OrdinalIgnoreCase)
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

        public static ApiLinha Aplicar(ApiLinha linha)
        {
            var extra = linha.CamposExtras ??= [];

            if (TentarPegarString(extra, "vehicle_desc", out var vd))
                Set(extra, "vehicle_desc", LimparColchetes(vd));
            if (TentarPegarString(extra, "vehicle_name", out var vn))
                Set(extra, "vehicle_name", LimparColchetes(vn));

            if (TentarPegarString(extra, "form_content", out var fc) && !string.IsNullOrWhiteSpace(fc))
            {
                fc = fc.Replace("\"\"", "\"");

                if (TentarDividirJson(fc, out var titulo, out var plano))
                {
                    if (!string.IsNullOrWhiteSpace(titulo) && !extra.ContainsKey("form_title")) Set(extra, "form_title", titulo);

                    var upperMap = plano.ToDictionary(kv => kv.Key.ToUpperInvariant(), kv => kv.Value);

                    foreach (var (canon, labels) in Campos)
                    {
                        foreach (var labelV in labels)
                        {
                            if (upperMap.TryGetValue(labelV.ToUpperInvariant(), out var valor))
                            {
                                extra[canon] = JsonSerializer.SerializeToElement(valor);
                                break;
                            }
                        }
                    }

                    Remover(extra, "form_content");
                }
            }

            return linha;
        }

        private static bool TentarDividirJson(string jsonCru, out string titulo, out Dictionary<string, string> plano)
        {
            titulo = string.Empty;
            plano = [];

            if (string.IsNullOrWhiteSpace(jsonCru)) return false;

            jsonCru = jsonCru.Trim();

            if (!jsonCru.StartsWith("{") && !jsonCru.StartsWith("["))
            {
                Console.WriteLine($"[TApontamento] Entrada não deve ser um json");
                return false;
            }

            if (jsonCru.Length >= 2 && jsonCru.StartsWith("\"") && jsonCru.EndsWith("\""))
                jsonCru = jsonCru[1..^1];

            jsonCru = jsonCru.Replace("[\"]", "\"\"").Replace(";", "");

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
                var nome = p.Name.ToLowerInvariant();

                if (nome is "title" or "form_title") continue;

                if (nome is "label" or "key" or "name")
                {
                    if (p.Value.ValueKind == JsonValueKind.String) label = p.Value.GetString();
                    continue;
                }

                if (nome is "value" or "answer" or "content" or "text")
                {
                    valor = JsonParaString(p.Value);
                    continue;
                }

                if (p.Value.ValueKind == JsonValueKind.String || p.Value.ValueKind == JsonValueKind.Number || p.Value.ValueKind == JsonValueKind.True || p.Value.ValueKind == JsonValueKind.False)
                {
                    plano[p.Name] = JsonParaString(p.Value);
                    continue;
                }

                if (p.Value.ValueKind == JsonValueKind.Object) 
                {
                    PlanificarObjeto(p.Value, plano);
                    continue;
                }

                if (p.Value.ValueKind == JsonValueKind.Array)
                {
                    bool eArrayDeObjetos = p.Value.GetArrayLength() > 0 && 
                                           p.Value.EnumerateArray().First().ValueKind == JsonValueKind.Object;

                    if (eArrayDeObjetos)
                    {
                        foreach (var elemento in p.Value.EnumerateArray()) 
                            PlanificarObjeto(elemento, plano);
                    }
                    else
                    {
                        plano[p.Name] = JsonParaString(p.Value);
                    }
                    
                    continue;
                }
            }

            if (!string.IsNullOrWhiteSpace(label)) plano[label] = valor ?? "";
        }

        private static string JsonParaString(JsonElement elemento)
        {
            return elemento.ValueKind switch
            {
                JsonValueKind.String => elemento.GetString() ?? "",
                JsonValueKind.Number => elemento.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => elemento.ToString()
            };
        }
    }
}