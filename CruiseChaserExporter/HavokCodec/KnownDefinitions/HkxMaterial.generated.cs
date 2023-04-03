namespace CruiseChaserExporter.HavokCodec.KnownDefinitions;

#pragma warning disable CS8618
#nullable enable

[System.CodeDom.Compiler.GeneratedCode("CruiseChaserExporter", "1.0.0.0")]
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
