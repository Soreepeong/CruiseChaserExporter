namespace CruiseChaserExporter.HkDefinitions;

public class HkxMaterial : HkxAttributeHolder {
    public string? Name;
    public List<HkxMaterialTextureStage> Stages;
    public float[] DiffuseColor;
    public float[] AmbientColor;
    public float[] SpecularColor;
    public float[] EmissiveColor;
    public List<HkxMaterial> SubMaterials;
    public HkReferencedObject? ExtraData;
    public List<HkxMaterialProperty> Properties;
}