using Newtonsoft.Json;

namespace CruiseChaserExporter.Gltf.Models;

public class GltfSampler {
    [JsonProperty("magFilter")] public GltfSamplerFilters MagFilter = GltfSamplerFilters.Linear;

    [JsonProperty("minFilter")] public GltfSamplerFilters MinFilter = GltfSamplerFilters.LinearMipmapLinear;
}
