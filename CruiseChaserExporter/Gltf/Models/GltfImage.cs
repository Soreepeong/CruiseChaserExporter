using Newtonsoft.Json;

namespace CruiseChaserExporter.Gltf.Models;

public class GltfImage {
    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string? Name;

    [JsonProperty("uri", NullValueHandling = NullValueHandling.Ignore)]
    public string? Uri;

    [JsonProperty("mimeType", NullValueHandling = NullValueHandling.Ignore)]
    public string? MimeType;

    [JsonProperty("bufferView", NullValueHandling = NullValueHandling.Ignore)]
    public int? BufferView;
}
