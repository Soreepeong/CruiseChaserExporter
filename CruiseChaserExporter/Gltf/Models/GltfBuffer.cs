using Newtonsoft.Json;

namespace CruiseChaserExporter.Gltf.Models;

public class GltfBuffer
{
    [JsonProperty("byteLength")] public long ByteLength;

    [JsonProperty("uri", NullValueHandling = NullValueHandling.Ignore)]
    public string? Uri;
}