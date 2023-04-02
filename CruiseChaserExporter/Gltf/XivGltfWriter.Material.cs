using BCnEncoder.Shared;
using CruiseChaserExporter.Gltf.Models;
using Lumina.Data.Files;
using Lumina.Data.Parsing;
using Lumina.Models.Materials;

namespace CruiseChaserExporter.Gltf;

public partial class XivGltfWriter {
    private unsafe int WriteMaterial(Material xivMaterial) {
        var material = new GltfMaterial {
            Name = xivMaterial.File?.FilePath.Path,
            DoubleSided = true,
            PbrMetallicRoughness = new(),
        };
        
        var xivTextureMap = new Dictionary<TextureUsage, Tuple<int, int, ColorRgba32[]>>();
        foreach (var xivTexture in xivMaterial.Textures) {
            var texbuf = _gameData.GetFile<TexFile>(xivTexture.TexturePath)!.TextureBuffer;
            var data = texbuf.Filter(format: TexFile.TextureFormat.B8G8R8A8).RawData;
            var data32 = new ColorRgba32[data.Length / 4];
            fixed (void* src = data, dst = data32)
				Buffer.MemoryCopy(src, dst, data.Length, data.Length);
            xivTextureMap[xivTexture.TextureUsageRaw] = Tuple.Create(texbuf.Width, texbuf.Height, data32);
        }

        switch (xivMaterial.ShaderPack) {
            case "character.shpk": {
                if (xivTextureMap.TryGetValue(TextureUsage.SamplerNormal, out var normal)) {
                    var diffuse = new ColorRgba32[normal.Item3.Length];
                    var specular = new ColorRgba32[normal.Item3.Length];
                    var emission = new ColorRgba32[normal.Item3.Length];

                    var colorSetInfo = xivMaterial.File!.ColorSetInfo;

                    for (var i = 0; i < normal.Item3.Length; i++) {
	                    var normalPixel = normal.Item3[i];

						//var b = (Math.Clamp(normalPixel.B, (byte)0, (byte)128) * 255) / 128;
						var colorSetIndex1 = (normalPixel.a / 17) * 16;
						var colorSetBlend = (normalPixel.a % 17) / 17.0;
						//var colorSetIndex2 = (((normalPixel.A / 17) + 1) % 16) * 16;
						var colorSetIndexT2 = (normalPixel.a / 17);
						var colorSetIndex2 = (colorSetIndexT2 >= 15 ? 15 : colorSetIndexT2 + 1) * 16;

						normal.Item3[i] = new(normalPixel.r, normalPixel.g, 255, 255);

						diffuse[i] = ColourBlend(
							UInt16To8BitColour(colorSetInfo.Data[colorSetIndex1 + 0]),
							UInt16To8BitColour(colorSetInfo.Data[colorSetIndex1 + 1]),
							UInt16To8BitColour(colorSetInfo.Data[colorSetIndex1 + 2]),
							UInt16To8BitColour(colorSetInfo.Data[colorSetIndex2 + 0]),
							UInt16To8BitColour(colorSetInfo.Data[colorSetIndex2 + 1]),
							UInt16To8BitColour(colorSetInfo.Data[colorSetIndex2 + 2]),
							normalPixel.b,
							colorSetBlend
						);

						specular[i] = ColourBlend(
							UInt16To8BitColour(colorSetInfo.Data[colorSetIndex1 + 4]),
							UInt16To8BitColour(colorSetInfo.Data[colorSetIndex1 + 5]),
							UInt16To8BitColour(colorSetInfo.Data[colorSetIndex1 + 6]),
							UInt16To8BitColour(colorSetInfo.Data[colorSetIndex2 + 4]),
							UInt16To8BitColour(colorSetInfo.Data[colorSetIndex2 + 5]),
							UInt16To8BitColour(colorSetInfo.Data[colorSetIndex2 + 6]),
							255,
							colorSetBlend
						);

						emission[i] = ColourBlend(
							UInt16To8BitColour(colorSetInfo.Data[colorSetIndex1 + 8]),
							UInt16To8BitColour(colorSetInfo.Data[colorSetIndex1 + 9]),
							UInt16To8BitColour(colorSetInfo.Data[colorSetIndex1 + 10]),
							UInt16To8BitColour(colorSetInfo.Data[colorSetIndex2 + 8]),
							UInt16To8BitColour(colorSetInfo.Data[colorSetIndex2 + 9]),
							UInt16To8BitColour(colorSetInfo.Data[colorSetIndex2 + 10]),
							255,
							colorSetBlend
						);
                    }
                }

                break;
            }
        }

        var num = 0;
        foreach (var (k, v) in xivTextureMap) {
            switch (k)
            {
                case TextureUsage.SamplerColorMap0:
                case TextureUsage.SamplerDiffuse:
                    material.PbrMetallicRoughness ??= new();
                    material.PbrMetallicRoughness.BaseColorTexture = new() {
                        Index = AddTexture($"diffuse_{num}.png", v.Item1, v.Item2, v.Item3, SourceAlphaModes.Enable),
                    };
                    break;
                case TextureUsage.SamplerNormalMap0:
                case TextureUsage.SamplerNormal:
                    material.NormalTexture = new() {
                        Index = AddTexture($"normal_{num}.png", v.Item1, v.Item2, v.Item3, SourceAlphaModes.Enable),
                    };
                    break;
                case TextureUsage.SamplerSpecularMap0:
                case TextureUsage.SamplerSpecular:
	                _root.ExtensionsUsed.Add("KHR_materials_specular");
                    material.Extensions ??= new();
                    material.Extensions.KhrMaterialsSpecular ??= new();
                    material.Extensions.KhrMaterialsSpecular.SpecularColorTexture = new() {
                        Index = AddTexture($"specular_{num}.png", v.Item1, v.Item2, v.Item3, SourceAlphaModes.Enable),
                    };
                    break;
                case TextureUsage.SamplerWaveMap:
                    material.OcclusionTexture = new() {
                        Index = AddTexture($"occlusion_{num}.png", v.Item1, v.Item2, v.Item3, SourceAlphaModes.Enable),
                    };
                    break;
                case TextureUsage.SamplerReflection:
                    material.EmissiveTexture = new() {
                        Index = AddTexture($"emissive_{num}.png", v.Item1, v.Item2, v.Item3, SourceAlphaModes.Enable),
                    };
                    break;
                default:
                    Console.WriteLine("Fucked shit, got unhandled TextureUsage {0}: {1}", k, v);
                    break;
            }

            num++;
        }

        return AddMaterial(material);
    }
    
    private static ColorRgba32 ColourBlend(byte xr, byte xg, byte xb, byte yr, byte yg, byte yb, byte a, double xBlendScalar) =>
	    new(
		    (byte)Math.Max(0, Math.Min(255, (int)Math.Round(yr * xBlendScalar + xr * (1 - xBlendScalar)))),
		    (byte)Math.Max(0, Math.Min(255, (int)Math.Round(yg * xBlendScalar + xg * (1 - xBlendScalar)))),
		    (byte)Math.Max(0, Math.Min(255, (int)Math.Round(yb * xBlendScalar + xb * (1 - xBlendScalar)))),
		    a
	    );

    private static byte UInt16To8BitColour(ushort s) =>
	    (byte)Math.Max(0, Math.Min(255, (int)Math.Floor((float)BitConverter.UInt16BitsToHalf(s) * 256)));
}