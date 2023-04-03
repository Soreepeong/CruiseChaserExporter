using Newtonsoft.Json;

namespace CruiseChaserExporter.Gltf.Models;

public class GltfMesh {
    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string? Name;

    [JsonProperty("primitives")] public List<GltfMeshPrimitive> Primitives = new();
}
