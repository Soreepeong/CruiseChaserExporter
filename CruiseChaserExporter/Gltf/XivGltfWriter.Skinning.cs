using System.Numerics;
using CruiseChaserExporter.ComposedModel;
using CruiseChaserExporter.Gltf.Models;

namespace CruiseChaserExporter.Gltf; 

public partial class XivGltfWriter {
    private bool WriteSkinsOrLogError(
        out GltfSkin skin,
        string name,
        Bone rootBone) {
        
        var boneStack = new List<Bone> {rootBone};
        var orderedInverseBindMatrices = new List<Matrix4x4>();
        var orderedNodeIndices = new List<int>();

        while (boneStack.Any()) {
            var bone = boneStack[^1];
            boneStack.RemoveAt(boneStack.Count - 1);
            boneStack.AddRange(bone.Children);
            
            var nodeIndex = _boneToNodeIndex[bone] = AddNode(new() {
                Name = bone.Name,
                Scale = (bone.Scale - Vector3.One).LengthSquared() > 0.000001
                    ? new List<float> {bone.Scale.X, bone.Scale.Y, bone.Scale.Z}
                    : null,
                Translation = bone.Translation != Vector3.Zero
                    ? new List<float> {bone.Translation.X, bone.Translation.Y, bone.Translation.Z}
                    : null,
                Rotation = bone.Rotation != Quaternion.Identity
                    ? new List<float> {bone.Rotation.X, bone.Rotation.Y, bone.Rotation.Z, bone.Rotation.W}
                    : null,
            });
            
            _boneToBoneIndex[bone] = orderedNodeIndices.Count;
            _boneByName[bone.Name] = bone;
            orderedInverseBindMatrices.Add(bone.InverseBindPoseMatrix);
            orderedNodeIndices.Add(nodeIndex);

            if (bone.Parent is not null)
                _root.Nodes[_boneToNodeIndex[bone.Parent]].Children.Add(nodeIndex);
        }

        var baseName = $"{name}/inverseBindMatrix";
        var inverseBindMatricesAccessor =
            GetAccessorOrDefault(baseName, 0, orderedInverseBindMatrices.Count)
            ?? AddAccessor(baseName, -1, null, orderedInverseBindMatrices
                .Select(NormalizeTransformationMatrix)
                .ToArray());

        skin = new() {
            InverseBindMatrices = inverseBindMatricesAccessor,
            Joints = orderedNodeIndices,
            Name = $"{name}/skin",
        };
        return true;
    }
}
