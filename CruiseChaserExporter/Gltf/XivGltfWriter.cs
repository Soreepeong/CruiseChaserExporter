using CruiseChaserExporter.Gltf.Models;
using CruiseChaserExporter.HavokCodec.KnownDefinitions;
using CruiseChaserExporter.Util;
using Lumina;
using Lumina.Models.Models;

namespace CruiseChaserExporter.Gltf;

public partial class XivGltfWriter {
    protected readonly TaggedLogger Log;
    private readonly bool _writeText;
    private readonly bool _writeBinary;
    private readonly GameData _gameData;

    protected GltfRoot _root = new();
    private readonly List<byte[]> _bytesList = new();

    private int _currentOffset;

    public XivGltfWriter(string logTag, bool writeText, bool writeBinary, GameData gameData) {
        Log = new(logTag);
        _writeText = writeText;
        _writeBinary = writeBinary;
        _gameData = gameData;

        Reset("Scene");
    }

    public void Reset(string sceneName) {
        _bytesList.Clear();
        _currentOffset = 0;
        _root = new();
        _root.Scenes.Add(new() {
            Name = sceneName,
        });
    }

    public bool AddModel(
        Model xivModel,
        HkRootLevelContainer hkSkeletonRoot,
        Dictionary<string, HkRootLevelContainer> hkAnimationRoots,
        bool omitSkins = false) {
        if (!CreateModelNode(out var node, xivModel, hkSkeletonRoot, hkAnimationRoots, omitSkins))
            return false;

        CurrentScene.Nodes.Add(AddNode(node));
        return true;
    }

    public void Save(string referencePath) {
        if (_writeBinary) {
            var glbf = new FileInfo($"{referencePath}.glb");
            using (var glb = glbf.Open(FileMode.Create, FileAccess.Write))
                CompileToBinary(glb);
            Log.I($"Saved: {glbf.Name} in {glbf.DirectoryName}");
        }

        if (_writeText) {
            var gltfFile = new FileInfo($"{referencePath}.gltf");
            var glbFile = new FileInfo($"{referencePath}.bin");
            using (var gltf = gltfFile.Open(FileMode.Create, FileAccess.Write))
            using (var bin = glbFile.Open(FileMode.Create, FileAccess.Write))
                CompileToPair(Path.GetFileName(bin.Name), gltf, bin);
            Log.I($"Saved: {gltfFile.Name} and {glbFile.Name} in {gltfFile.DirectoryName}");
        }
    }
}
