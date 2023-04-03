namespace CruiseChaserExporter.HkDefinitions;

public class HkxMaterial : HkxAttributeHolder {
    public string? Name;
    public HkxMaterialTextureStage[] Stages;
    public float[] DiffuseColor;
    public float[] AmbientColor;
    public float[] SpecularColor;
    public float[] EmissiveColor;
    public HkxMaterial[] SubMaterials;
    public HkReferencedObject? ExtraData;
    public HkxMaterialProperty[] Properties;
}
