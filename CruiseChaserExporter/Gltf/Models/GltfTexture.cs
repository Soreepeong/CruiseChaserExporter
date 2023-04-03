using Newtonsoft.Json;

namespace CruiseChaserExporter.Gltf.Models;

public class GltfTexture {
    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string? Name;

    [JsonProperty("source", NullValueHandling = NullValueHandling.Ignore)]
    public int? Source;

    [JsonProperty("extensions", NullValueHandling = NullValueHandling.Ignore)]
    public GltfExtensions? Extensions;
}
