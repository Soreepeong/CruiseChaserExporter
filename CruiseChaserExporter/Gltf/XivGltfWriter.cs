using System.Diagnostics;
using CruiseChaserExporter.Animation;
using CruiseChaserExporter.ComposedModel;
using CruiseChaserExporter.Gltf.Models;
using CruiseChaserExporter.Util;
using Lumina;

namespace CruiseChaserExporter.Gltf;

public partial class XivGltfWriter {
    protected readonly TaggedLogger Log;
    private readonly bool _writeText;
    private readonly bool _writeBinary;
    private readonly GameData _gameData;

    protected GltfRoot _root = new();
    private readonly List<byte[]> _bytesList = new();
    private readonly Dictionary<Bone, int> _boneToRootNodeIndex = new();
    private readonly Dictionary<Bone, int> _boneToBoneIndex = new();
    private readonly Dictionary<Bone, int> _boneToNodeIndex = new();
    private readonly Dictionary<string, Bone> _boneByName = new();

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
        _boneToRootNodeIndex.Clear();
        _boneToBoneIndex.Clear();
        _boneToNodeIndex.Clear();
        _boneByName.Clear();
        _currentOffset = 0;
        _root = new();
        _root.Scenes.Add(new() {
            Name = sceneName,
        });
    }

    public bool AddSkin(string name, Bone rootBone) {
        if (rootBone.Parent is not null)
            return Log.E<bool>(@"Root bone must not have a parent.");

        if (!WriteSkinsOrLogError(out var skin, name, rootBone))
            return false;

        CurrentScene.Nodes.Add(_boneToRootNodeIndex[rootBone] = AddNode(new() {
            Name = name,
            Children = new() {_boneToBoneIndex[rootBone]},
            Skin = AddSkin(skin),
            Mesh = AddMesh(new() {Name = $"{name}/mesh"}),
        }));
        return true;
    }

    public bool AddAnimation(string name, IAnimation animation, IEnumerable<Bone> bones)
        => WriteAnimation(name, animation, bones.Select(x => _boneToNodeIndex[x]).ToList());

    public bool AddModel(string name, SourceModel model) {
        if (model.RootBone is null) {
            var node = new GltfNode {Name = name};
            var mesh = new GltfMesh {Name = $"{name}/mesh"};

            if (!WritePrimitives(mesh.Primitives, model.Model))
                return false;

            node.Mesh = AddMesh(mesh);

            CurrentScene.Nodes.Add(AddNode(node));
            return true;
        } else {
            var bone = model.RootBone;
            while (bone.Parent is not null)
                bone = bone.Parent;

            var node = _root.Nodes[_boneToRootNodeIndex[bone]];
            Debug.Assert(node.Mesh != null);
            var mesh = _root.Meshes[node.Mesh.Value];
            return WritePrimitives(mesh.Primitives, model.Model);
        }
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
