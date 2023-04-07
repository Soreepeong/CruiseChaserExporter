using CruiseChaserExporter.XivStruct;
using Lumina.Models.Models;

namespace CruiseChaserExporter.ComposedModel;

public class SourceModel {
    private List<Bone>? _bones;
    private Bone? _rootBone;
    public readonly Model Model;
    public readonly AnimDictKey AnimationKey;

    public SourceModel(Model model, List<Bone>? bones, AnimDictKey animationKey) {
        Model = model;
        _bones = bones;
        AnimationKey = animationKey;
        _rootBone = bones?.Single(x => x.Parent is null);
    }

    public List<Bone>? Bones {
        get => _bones;
        set {
            _bones = value;
            _rootBone = value?.Single(x => x.Parent is null);
        }
    }

    public Bone? RootBone {
        get => _rootBone;
        set {
            if (value is null) {
                if (_bones is not null)
                    throw new ArgumentException(null, nameof(value));
                return;
            }

            if (_bones is null)
                throw new InvalidOperationException();

            _bones = _bones.Select(x => x == _rootBone ? value : x).ToList();

            var oldChildren = _rootBone?.Children;
            _rootBone = value;
            
            if (oldChildren is not null) {
                foreach (var child in oldChildren)
                    child.Parent = value;
            }
        }
    }

    public override string ToString() => $"SourceModel \"{Path.GetFileNameWithoutExtension(Model.File!.FilePath.Path)}\"";
}