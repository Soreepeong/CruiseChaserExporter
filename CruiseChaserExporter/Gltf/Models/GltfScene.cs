using Newtonsoft.Json;

namespace CruiseChaserExporter.Gltf.Models;

public class GltfScene {
    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string? Name;

    [JsonProperty("nodes")] public List<int> Nodes = new();
}
