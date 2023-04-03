using Newtonsoft.Json;

namespace CruiseChaserExporter.Gltf.Models;

public class GltfExtensions {
    [JsonProperty("KHR_materials_specular", NullValueHandling = NullValueHandling.Ignore)]
    public GltfExtensionKhrMaterialsSpecular? KhrMaterialsSpecular;

    [JsonProperty("MSFT_texture_dds", NullValueHandling = NullValueHandling.Ignore)]
    public GltfExtensionMsftTextureDds? MsftTextureDds;
}
