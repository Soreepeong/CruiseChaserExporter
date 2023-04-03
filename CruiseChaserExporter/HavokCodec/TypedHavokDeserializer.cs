using CruiseChaserExporter.HavokCodec.HavokTagfile;

namespace CruiseChaserExporter.HavokCodec;

public class TypedHavokDeserializer {
    private readonly Dictionary<Definition, Type> _defDict;

    public TypedHavokDeserializer(string rootNamespace, Dictionary<Tuple<string, int>, Definition> definitions) {
        var typeDict = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(x => x.Namespace == rootNamespace)
            .ToDictionary(x => x.Name, x => x);
        _defDict = definitions
            .GroupBy(x => x.Key.Item1, (_, v) => v.MaxBy(y => y.Key.Item2))
            .ToDictionary(x => x.Value, x => typeDict[NormalizeName(x.Value.Name)]);
    }

    public TypedHavokDeserializer(Type exampleType, Dictionary<Tuple<string, int>, Definition> definitions)
        : this(exampleType.Namespace!, definitions) { }

    public T Deserialize<T>(Node node) => Deserializer.Deserialize<T>(node, _defDict, NormalizeName);

    public static void WriteGeneratedCode(IEnumerable<Definition> definitions) {
        foreach (var def in definitions)
            File.WriteAllText($"{NormalizeName(def.Name)}.generated.cs", def.GenerateCSharpCode());
    }
    
    public static string NormalizeName(string x) => char.IsUpper(x[0]) ? x : char.ToUpperInvariant(x[0]) + x[1..];
}
