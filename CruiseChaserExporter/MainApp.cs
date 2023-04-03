using CruiseChaserExporter.Gltf;
using CruiseChaserExporter.HavokCodec;
using CruiseChaserExporter.HavokCodec.HavokTagfile;
using CruiseChaserExporter.HavokCodec.KnownDefinitions;
using CruiseChaserExporter.XivStruct;
using Lumina.Models.Models;

namespace CruiseChaserExporter;

public static class MainApp {
    public static XivGltfWriter GltfFromMonster(
        int modelId, int bodyId, int variantId,
        Lumina.GameData lumina, IEnumerable<string> paths
    ) {
        const int boneId = 1;
        var basePath = $"chara/monster/m{modelId:D4}/";
        var modelPath = $"{basePath}obj/body/b{bodyId:D4}/model/m{modelId:D4}b{bodyId:D4}.mdl";
        var skelPath = $"{basePath}skeleton/base/b{boneId:D4}/skl_m{modelId:D4}b{boneId:D4}.sklb";

        var allPaths = paths
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x) && x.StartsWith(basePath))
            .GroupBy(Path.GetExtension, (k, v) => Tuple.Create(k, v.Order().ToArray()))
            .ToDictionary(x => x.Item1!.ToLowerInvariant(), x => x.Item2);
        
        var definitions = new Dictionary<Tuple<string, int>, Definition>();
        var nodesSklb = Parser.Parse(lumina.GetFile<SklbFile>(skelPath)!.HavokData, definitions);
        var animNodes = allPaths[".pap"].ToDictionary(
            path => path,
            path => Parser.Parse(lumina.GetFile<PapFile>(path)!.HavokData, definitions));

        // TypedHavokDeserializer.WriteGeneratedCode(definitions.Values);

        var thd = new TypedHavokDeserializer(typeof(HkRootLevelContainer), definitions);
        var sklb = thd.Deserialize<HkRootLevelContainer>(nodesSklb);
        var anims = animNodes.ToDictionary(x => x.Key, x => thd.Deserialize<HkRootLevelContainer>(x.Value));

        var xivModel = new Model(lumina, modelPath, Model.ModelLod.High, variantId);

        var writer = new XivGltfWriter("GLTF", false, true, lumina);
        if (!writer.AddModel(xivModel, sklb, anims))
            throw new InvalidOperationException();
        return writer;
    }

    public static void Main() {
        var lumina = new Lumina.GameData(
            @"C:\Program Files (x86)\SquareEnix\FINAL FANTASY XIV - A Realm Reborn\game\sqpack");
        
        // Cruise Chaser (A11)
        GltfFromMonster(361, 1, 1, lumina, Resources.m0361.Split("\n")).Save("m0361_b0001_v0001");
        
        // Mustadio (Orbonne)
        GltfFromMonster(361, 3, 1, lumina, Resources.m0361.Split("\n")).Save("m0361_b0003_v0001");
        
        // Construct 14 (Ridorana)
        GltfFromMonster(489, 1, 1, lumina, Resources.m0489.Split("\n")).Save("m0489_b0001_v0001");
    }
}
