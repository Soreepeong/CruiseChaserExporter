using Newtonsoft.Json;

namespace CruiseChaserExporter.Gltf.Models;

public class GltfTextureInfo
{
    [JsonProperty("index")]
    public int Index;

    [JsonProperty("texCoord", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public int TexCoord;
}