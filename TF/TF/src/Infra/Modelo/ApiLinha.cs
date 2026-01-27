using System.Text.Json;
using System.Text.Json.Serialization;

namespace TF.src.Infra.Modelo
{
    public class ApiLinha
    {
        [JsonPropertyName("UpdatedAtIso")]
        public string? UpdatedAtIso { get; set; }

        [JsonPropertyName("updated_at")]
        public string? UpdatedAtSnake { set => UpdatedAtIso = value; }

        [JsonPropertyName("updatedAt")]
        public string? UpdatedAtCamel { set => UpdatedAtIso = value; }

        [JsonPropertyName("modified_at")]
        public string? ModifiedAtSnake { set => UpdatedAtIso = value; }

        [JsonPropertyName("modifiedAt")]
        public string? ModifiedAtCamel { set => UpdatedAtIso = value; }

        [JsonPropertyName("timestamp")]
        public string? Timestamp { set => UpdatedAtIso = value; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? CamposExtras { get; set; }

        [JsonIgnore]
        public string? HighWaterMark { get; set; }
    }
}