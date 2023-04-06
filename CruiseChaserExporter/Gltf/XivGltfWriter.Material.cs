using BCnEncoder.Shared;
using CruiseChaserExporter.Gltf.Models;
using Lumina.Data.Files;
using Lumina.Data.Parsing;
using Lumina.Models.Materials;

namespace CruiseChaserExporter.Gltf;

public partial class XivGltfWriter {
    private int WriteMaterial(Material xivMaterial) {
        var material = new GltfMaterial {
            Name = Path.GetFileNameWithoutExtension(xivMaterial.File?.FilePath.Path),
            DoubleSided = true,
            PbrMetallicRoughness = new(),
        };

        var xivTextureMap = new Dictionary<TextureUsage, Tuple<int, int, ColorRgba32[]>>();
        foreach (var xivTexture in xivMaterial.Textures) {
            if (xivTexture.TexturePath == "dummy.tex")
                continue;

            var texbuf = _gameData.GetFile<TexFile>(xivTexture.TexturePath)!.TextureBuffer;
            var data = texbuf.Filter(format: TexFile.TextureFormat.B8G8R8A8).RawData;
            var data32 = new ColorRgba32[data.Length / 4];
            for (int i = 0, j = 0; j < data32.Length; j++) {
                data32[j].b = data[i++];
                data32[j].g = data[i++];
                data32[j].r = data[i++];
                data32[j].a = data[i++];
            }

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
                        var colorSetBlend = (normalPixel.a % 17) / 17f;
                        //var colorSetIndex2 = (((normalPixel.A / 17) + 1) % 16) * 16;
                        var colorSetIndexT2 = (normalPixel.a / 17);
                        var colorSetIndex2 = (colorSetIndexT2 >= 15 ? 15 : colorSetIndexT2 + 1) * 16;

                        normal.Item3[i] = new(normalPixel.r, normalPixel.g, 255, 255);

                        diffuse[i] = Blend(colorSetInfo, colorSetIndex1, colorSetIndex2, normalPixel.b, colorSetBlend);
                        specular[i] = Blend(colorSetInfo, colorSetIndex1, colorSetIndex2, 255, colorSetBlend);
                        emission[i] = Blend(colorSetInfo, colorSetIndex1, colorSetIndex2, 255, colorSetBlend);
                    }

                    xivTextureMap.TryAdd(TextureUsage.SamplerDiffuse,
                        Tuple.Create(normal.Item1, normal.Item2, diffuse));
                    xivTextureMap.TryAdd(TextureUsage.SamplerSpecular,
                        Tuple.Create(normal.Item1, normal.Item2, specular));
                    xivTextureMap.TryAdd(TextureUsage.SamplerReflection,
                        Tuple.Create(normal.Item1, normal.Item2, emission));
                }

                if (xivTextureMap.TryGetValue(TextureUsage.SamplerMask, out var mask) &&
                    xivTextureMap.TryGetValue(TextureUsage.SamplerSpecular, out var specularMap)) {
                    var occlusion = new ColorRgba32[mask.Item3.Length];

                    for (var i = 0; i < mask.Item3.Length; i++) {
                        var maskPixel = mask.Item3[i];
                        var specularPixel = specularMap.Item3[i];

                        specularMap.Item3[i].r = (byte) (specularPixel.r * Math.Pow(maskPixel.g / 255f, 2));
                        specularMap.Item3[i].g = (byte) (specularPixel.g * Math.Pow(maskPixel.g / 255f, 2));
                        specularMap.Item3[i].b = (byte) (specularPixel.b * Math.Pow(maskPixel.g / 255f, 2));
                        occlusion[i] = new(maskPixel.r, maskPixel.r, maskPixel.r, 255);
                    }

                    xivTextureMap.Add(TextureUsage.SamplerWaveMap,
                        Tuple.Create(mask.Item1, mask.Item2, occlusion));
                }

                break;
            }
        }

        foreach (var (k, v) in xivTextureMap) {
            switch (k) {
                case TextureUsage.SamplerColorMap0:
                case TextureUsage.SamplerDiffuse:
                    (material.PbrMetallicRoughness ??= new()).BaseColorTexture ??= new() {
                        Index = AddTexture($"{material.Name}/diffuse", v.Item1, v.Item2, v.Item3,
                            SourceAlphaModes.Enable),
                    };
                    break;
                case TextureUsage.SamplerNormalMap0:
                case TextureUsage.SamplerNormal:
                    material.NormalTexture ??= new() {
                        Index = AddTexture($"{material.Name}/normal", v.Item1, v.Item2, v.Item3,
                            SourceAlphaModes.Enable),
                    };
                    break;
                case TextureUsage.SamplerSpecularMap0:
                case TextureUsage.SamplerSpecular:
                    _root.ExtensionsUsed.Add("KHR_materials_specular");
                    ((material.Extensions ??= new()).KhrMaterialsSpecular ??= new()).SpecularColorTexture ??= new() {
                        Index = AddTexture($"{material.Name}/specular", v.Item1, v.Item2, v.Item3,
                            SourceAlphaModes.Enable),
                    };
                    break;
                case TextureUsage.SamplerWaveMap:
                    material.OcclusionTexture ??= new() {
                        Index = AddTexture($"{material.Name}/occlusion", v.Item1, v.Item2, v.Item3,
                            SourceAlphaModes.Enable),
                    };
                    break;
                case TextureUsage.SamplerReflection:
                    material.EmissiveTexture ??= new() {
                        Index = AddTexture($"{material.Name}/emissive", v.Item1, v.Item2, v.Item3,
                            SourceAlphaModes.Enable),
                    };
                    break;
                default:
                    Log.W("Unsupported TextureUsage {0}: {1}", k, v);
                    break;
            }
        }

        return AddMaterial(material);
    }

    private static byte Blend(byte x, byte y, double scaler) =>
        (byte) Math.Clamp((x * (1 - scaler) + y * scaler) / byte.MaxValue, 0, byte.MaxValue);

    private static ColorRgba32 Blend(ColorRgba32 x, ColorRgba32 y, byte a, double scaler) =>
        new(Blend(x.r, y.r, scaler), Blend(x.g, y.g, scaler), Blend(x.b, y.b, scaler), a);

    private static unsafe ColorRgba32 ColorFromSet(ColorSetInfo colorSetInfo, int colorSetIndex) =>
        new(UInt16To8BitColour(colorSetInfo.Data[colorSetIndex + 0]),
            UInt16To8BitColour(colorSetInfo.Data[colorSetIndex + 1]),
            UInt16To8BitColour(colorSetInfo.Data[colorSetIndex + 2]),
            255);

    private static ColorRgba32 Blend(ColorSetInfo colorSetInfo, int colorSetIndex1, int colorSetIndex2,
        byte alpha, double scaler) => Blend(
        ColorFromSet(colorSetInfo, colorSetIndex1),
        ColorFromSet(colorSetInfo, colorSetIndex2),
        alpha,
        scaler
    );

    private static byte UInt16To8BitColour(ushort s) =>
        (byte) Math.Clamp(MathF.Floor((float) BitConverter.UInt16BitsToHalf(s) * 256), 0, byte.MaxValue);
}
