using CruiseChaserExporter.Gltf;
using CruiseChaserExporter.HkDefinitions;
using CruiseChaserExporter.HkTagfile;
using CruiseChaserExporter.XivStruct;
using Lumina.Models.Models;

namespace CruiseChaserExporter;

public static class MainApp {
    private static string NormalizeName(string x) => char.IsUpper(x[0]) ? x : char.ToUpperInvariant(x[0]) + x[1..];

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

        TagfileParser.Parse(out var nodesSklb, out var definitionsSklb,
            new(new MemoryStream(lumina.GetFile<SklbFile>(skelPath)!.HavokData)));
        var definitions = definitionsSklb.ToList();
        var animNodes = new Dictionary<string, HkNode>();
        foreach (var path in allPaths[".pap"]) {
            TagfileParser.Parse(out var nodesPap, out var definitionPap,
                new(new MemoryStream(lumina.GetFile<PapFile>(path)!.HavokData)));
            definitions.AddRange(definitionPap);
            animNodes[path] = nodesPap;
        }

        definitions = definitions.DistinctBy(x => Tuple.Create(x.Name, x.Version)).ToList();
        // foreach (var def in definitions)
        //     Console.WriteLine(def.GenerateCSharpCode(NormalizeName));

        var typeDict = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(x => x.Namespace == typeof(HkRootLevelContainer).Namespace)
            .ToDictionary(x => x.Name, x => x);
        var defDict = definitions.ToDictionary(x => x, x => typeDict[NormalizeName(x.Name)]);

        var sklb = TagfileDeserializer.Deserialize<HkRootLevelContainer>(nodesSklb, defDict, NormalizeName);
        var anims = animNodes.ToDictionary(x => x.Key,
            x => TagfileDeserializer.Deserialize<HkRootLevelContainer>(x.Value, defDict, NormalizeName));

        var xivModel = new Model(lumina, modelPath, Model.ModelLod.High, variantId);

        var writer = new XivGltfWriter("GLTF", false, true, lumina);
        if (!writer.AddModel(xivModel, sklb, anims))
            throw new InvalidOperationException();
        return writer;
    }

    public static void Main() {
        var lumina = new Lumina.GameData(
            @"C:\Program Files (x86)\SquareEnix\FINAL FANTASY XIV - A Realm Reborn\game\sqpack");
        GltfFromMonster(361, 1, 1, lumina, Resources.m0361.Split("\n")).Save("m0361_b0001_v0001");
        // GltfFromMonster(361, 2, 1, lumina, Resources.m0361.Split("\n")).Save("m0361_b0002_v0001");
        // GltfFromMonster(361, 3, 1, lumina, Resources.m0361.Split("\n")).Save("m0361_b0003_v0001");
        // GltfFromMonster(489, 1, 1, lumina, Resources.m0489.Split("\n")).Save("m0489_b0001_v0001");
    }
}
