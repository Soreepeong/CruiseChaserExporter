using System.Numerics;

namespace CruiseChaserExporter.ComposedModel;

public class Bone {
    private Bone? _parent;
    private readonly HashSet<Bone> _children = new();
    
    public readonly string Name;
    public readonly Vector3 Translation;
    public readonly Quaternion Rotation;
    public readonly Vector3 Scale;

    private Matrix4x4? _transformationMatrix;
    private Matrix4x4? _bindPoseMatrix;
    private Matrix4x4? _inverseBindPoseMatrix;

    public Bone(Bone? parent, string name, Vector3 translation, Quaternion rotation, Vector3 scale) {
        Name = name;
        Translation = translation;
        Rotation = Quaternion.Normalize(rotation);
        Scale = scale;

        Parent = parent;
    }

    public Bone? Parent {
        get => _parent;
        set {
            if (_parent == value)
                return;

            _parent?._children.Remove(this);
            _parent = value;
            if (value is null)
                return;

            value._children.Add(this);
            ClearCachedMatrices();
        }
    }

    public IEnumerable<Bone> Children => _children;

    public override string ToString() => _children.Count switch {
        0 => $"Bone \"{Name}\" (leaf)",
        1 => $"Bone \"{Name}\" (1 child)",
        _ => $"Bone \"{Name}\" ({_children.Count} children)",
    };

    private void ClearCachedMatrices() {
        _transformationMatrix = _bindPoseMatrix = _inverseBindPoseMatrix = null;
        foreach (var child in _children)
            child.ClearCachedMatrices();
    }

    public Matrix4x4 TransformationMatrix => _transformationMatrix ??=
        Matrix4x4.CreateScale(Scale)
        * Matrix4x4.CreateFromQuaternion(Rotation)
        * Matrix4x4.CreateTranslation(Translation);

    public Matrix4x4 BindPoseMatrix => _bindPoseMatrix ??= Parent == null
        ? TransformationMatrix
        : TransformationMatrix * Parent.BindPoseMatrix;

    public Matrix4x4 InverseBindPoseMatrix => _inverseBindPoseMatrix ??=
        Matrix4x4.Invert(BindPoseMatrix, out var v)
            ? (_inverseBindPoseMatrix = v).Value
            : throw new InvalidDataException("Failed to calculate inverse bind pose matrix.");
}
