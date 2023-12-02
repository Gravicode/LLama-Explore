using System.Text.Json.Serialization;

namespace Microsoft.SemanticKernel.Handlebars;

public class ThreadModel
{

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("object")]
    public string Object { get; set; }
    
    [JsonPropertyName("created_at")]
    public int CreatedAt { get; set; }


    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; }
}
