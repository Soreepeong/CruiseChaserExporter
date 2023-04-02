using CruiseChaserExporter.Gltf;
using CruiseChaserExporter.HkDefinitions;
using CruiseChaserExporter.HkTagfile;
using Lumina.Data;
using Lumina.Data.Files;
using Lumina.Models.Models;

namespace CruiseChaserExporter; 

public class MainApp {
    private static byte[] GetHkxFromSklb(FileResource file) {
        if (BitConverter.ToUInt32(file.Data, 0) != 0x736B6C62)
            throw new InvalidDataException();

        var version = BitConverter.ToInt32(file.Data, 4);
        switch (version) {
            case 0x31333030:
                return file.Data[BitConverter.ToInt32(file.Data, 12)..];
            case 0x31333032:
                return file.Data[BitConverter.ToInt16(file.Data, 10)..];
            default:
                throw new InvalidDataException();
        }
    }

    private static byte[] GetHkxFromPap(FileResource file) {
        if (BitConverter.ToUInt32(file.Data, 0) != 0x20706170)
            throw new InvalidDataException();
        var havokDataOffset = BitConverter.ToInt32(file.Data, 18);
        var parametersOffset = BitConverter.ToInt32(file.Data, 22);
        return file.Data[havokDataOffset..(havokDataOffset + parametersOffset)];
    }

    private static string NormalizeName(string x) => char.IsUpper(x[0]) ? x : char.ToUpperInvariant(x[0]) + x[1..];
    
    public static void Main() {
        var lumina =
            new Lumina.GameData(@"C:\Program Files (x86)\SquareEnix\FINAL FANTASY XIV - A Realm Reborn\game\sqpack");

        TagfileParser.Parse(out var nodesSklb, out var definitionsSklb, new(new MemoryStream(GetHkxFromSklb(lumina.GetFile("chara/monster/m0361/skeleton/base/b0001/skl_m0361b0001.sklb")!))));
        TagfileParser.Parse(out var nodesPap, out var definitionPap, new(new MemoryStream(GetHkxFromPap(lumina.GetFile("chara/monster/m0361/animation/a0001/bt_common/idle_sp/idle_sp_1.pap")!))));
        var definitions = definitionsSklb.Concat(definitionPap)
            .DistinctBy(x => Tuple.Create(x.Name, x.Version))
            .ToList();
        // foreach (var def in definitions)
        //     Console.WriteLine(def.GenerateCSharpCode(NormalizeName));
        
        var typeDict = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(x => x.Namespace == typeof(HkRootLevelContainer).Namespace)
            .ToDictionary(x => x.Name, x => x);
        var defDict = definitions.ToDictionary(x => x, x => typeDict[NormalizeName(x.Name)]);
        
        var sklb = TagfileUnserializer.Unserialize<HkRootLevelContainer>(nodesSklb, defDict, NormalizeName);
        var pap = TagfileUnserializer.Unserialize<HkRootLevelContainer>(nodesPap, defDict, NormalizeName);

        var xivModel = new Model(lumina.GetFile<MdlFile>("chara/monster/m0361/obj/body/b0001/model/m0361b0001.mdl")!);

        var writer = new XivGltfWriter("GLTF", false, true, lumina);
        if (!writer.AddModel(xivModel, sklb))
            throw new InvalidOperationException();
        writer.Save("Z:/test");
    }
}