using Newtonsoft.Json;

namespace CruiseChaserExporter.Gltf.Models;

public class GltfMeshPrimitive {
    [JsonProperty("attributes")] public GltfMeshPrimitiveAttributes Attributes = new();

    [JsonProperty("indices", NullValueHandling = NullValueHandling.Ignore)]
    public int? Indices;

    [JsonProperty("material", NullValueHandling = NullValueHandling.Ignore)]
    public int? Material;
}
