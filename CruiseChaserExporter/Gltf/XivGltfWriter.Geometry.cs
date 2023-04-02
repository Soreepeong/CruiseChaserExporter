using System.Numerics;
using CruiseChaserExporter.Gltf.Models;
using CruiseChaserExporter.HkDefinitions;
using Lumina.Data.Files;
using Lumina.Models.Models;

namespace CruiseChaserExporter.Gltf;

public partial class XivGltfWriter {
    private bool WriteSkinOrLogError(out GltfSkin newSkin,
        List<int> boneIndexToNodeIndex,
        GltfNode rootNode,
        HkRootLevelContainer sklbRoot) {
        newSkin = null!;
        boneIndexToNodeIndex = new();

        if (!sklbRoot.NamedVariants.Any())
            return Log.W<bool>("Nothing is contained");

        if (sklbRoot.NamedVariants[0].Variant is not HkaAnimationContainer container)
            return Log.W<bool>("Contained element is not a hkaAnimationContainer");

        if (container.Skeletons.Count != 1)
            return Log.W<bool>("Skeleton count = {0} != 1", container.Skeletons.Count);

        var bones = container.Skeletons[0].Bones;
        var parentIndices = container.Skeletons[0].ParentIndices;
        var referencePoses = container.Skeletons[0].ReferencePose;

        var bindPoseMatrices = new Matrix4x4[parentIndices.Count];
        var inverseBindPoseMatrices = new Matrix4x4[parentIndices.Count];

        for (var i = 0; i < bones.Count; i++) {
            var bone = bones[i];
            var refPose = referencePoses[i];
            var parentIndex = parentIndices[i];

            if (bone.Name is null)
                return Log.E<bool>("{0}/#{1}: Bone name is null.", rootNode.Name, i);

            // TODO: verify
            var translation = new Vector3(refPose[0], refPose[1], refPose[2] /*, refPose[3] = 0 */);
            var rotation = new Quaternion(refPose[4], refPose[5], refPose[6], refPose[7]);
            var scale = new Vector3(refPose[8], refPose[9], refPose[10] /*, refPose[11] = 0 */);

            bindPoseMatrices[i] =
                Matrix4x4.CreateScale(scale)
                * Matrix4x4.CreateFromQuaternion(rotation)
                * Matrix4x4.CreateTranslation(translation);

            if (!Matrix4x4.Invert(bindPoseMatrices[i], out inverseBindPoseMatrices[i]))
                return Log.E<bool>("{0}/{1}: Failed to invert BindPoseMatrix.", rootNode.Name, bone.Name);

            boneIndexToNodeIndex.Add(AddNode(new() {
                Name = bone.Name,
                Scale = (scale - Vector3.One).LengthSquared() > 0.000001
                    ? new List<float> {scale.X, scale.Y, scale.Z}
                    : null,
                Translation = translation != Vector3.Zero
                    ? new List<float> {translation.X, translation.Y, translation.Z}
                    : null,
                Rotation = rotation != Quaternion.Identity
                    ? new List<float> {rotation.X, rotation.Y, rotation.Z, rotation.W}
                    : null,
            }));

            if (parentIndex == -1)
                rootNode.Children.Add(boneIndexToNodeIndex[i]);
            else
                _root.Nodes[boneIndexToNodeIndex[parentIndex]].Children.Add(boneIndexToNodeIndex[i]);
        }

        var baseName = $"{rootNode.Name}/inverseBindMatrix";
        var inverseBindMatricesAccessor =
            GetAccessorOrDefault(baseName, 0, bones.Count)
            ?? AddAccessor(baseName, -1, null, inverseBindPoseMatrices
                .Select(NormalizeTransformationMatrix)
                .ToArray());

        newSkin = new() {
            InverseBindMatrices = inverseBindMatricesAccessor,
            Joints = boneIndexToNodeIndex,
            Name = $"{rootNode.Name}/skin",
        };
        return true;
    }

    private bool WriteMeshOrLogError(
        out List<GltfMeshPrimitive> newPrimitives,
        string rootNodeName,
        Mesh xivMesh,
        ushort[] indexBufferArray,
        int indexBufferView,
        int materialIndex) {
        string baseName;

        GltfMeshPrimitiveAttributes accessors = new();

        baseName = $"{rootNodeName}/vertex";
        accessors.Position =
            GetAccessorOrDefault(baseName, 0, xivMesh.Vertices.Length)
            ?? AddAccessor(baseName, -1, GltfBufferViewTarget.ArrayBuffer,
                xivMesh.Vertices.Select(x => NormalizePosition(x.Position!.Value)).ToArray());

        baseName = $"{rootNodeName}/normal";
        if (xivMesh.Vertices[0].Normal is not null)
            accessors.Normal =
                GetAccessorOrDefault(baseName, 0, xivMesh.Vertices.Length)
                ?? AddAccessor(baseName, -1, GltfBufferViewTarget.ArrayBuffer,
                    xivMesh.Vertices.Select(x => NormalizeNormal(x.Normal!.Value)).ToArray());

        baseName = $"{rootNodeName}/tangent";
        if (xivMesh.Vertices[0].Tangent1 is not null)
            accessors.Tangent =
                GetAccessorOrDefault(baseName, 0, xivMesh.Vertices.Length)
                ?? AddAccessor(baseName, -1, GltfBufferViewTarget.ArrayBuffer,
                    xivMesh.Vertices.Select(x => NormalizeTangent(x.Tangent1!.Value)).ToArray());

        baseName = $"{rootNodeName}/color";
        if (xivMesh.Vertices[0].Color is not null)
            accessors.Color0 =
                GetAccessorOrDefault(baseName, 0, xivMesh.Vertices.Length)
                ?? AddAccessor(baseName, -1, GltfBufferViewTarget.ArrayBuffer,
                    xivMesh.Vertices.Select(x => x.Color!.Value).ToArray());

        baseName = $"{rootNodeName}/uv";
        if (xivMesh.Vertices[0].UV is not null)
            accessors.TexCoord0 =
                GetAccessorOrDefault(baseName, 0, xivMesh.Vertices.Length)
                ?? AddAccessor(baseName, -1, GltfBufferViewTarget.ArrayBuffer,
                    xivMesh.Vertices.Select(x => NormalizeUv(x.UV!.Value)).ToArray());

        if (xivMesh.Vertices[0].BlendWeights is not null) {
            baseName = $"{rootNodeName}/bone/weights";
            accessors.Weights0 =
                GetAccessorOrDefault(baseName, 0, xivMesh.Vertices.Length)
                ?? AddAccessor(baseName, -1, GltfBufferViewTarget.ArrayBuffer,
                    xivMesh.Vertices.Select(x => x.BlendWeights!.Value).ToArray());

            baseName = $"{rootNodeName}/bone/joints";
            accessors.Joints0 =
                GetAccessorOrDefault(baseName, 0, xivMesh.Vertices.Length)
                ?? AddAccessor(baseName, -1, GltfBufferViewTarget.ArrayBuffer,
                    xivMesh.Vertices.Select(x => new TypedVec4<ushort>(
                        xivMesh.BoneTable[x.BlendIndices[0]],
                        xivMesh.BoneTable[x.BlendIndices[1]],
                        xivMesh.BoneTable[x.BlendIndices[2]],
                        xivMesh.BoneTable[x.BlendIndices[3]])).ToArray());
        }

        newPrimitives = xivMesh.Submeshes
            .Select(v => new GltfMeshPrimitive {
                Attributes = accessors,
                Indices = GetAccessorOrDefault(baseName, (int) v.IndexOffset, (int) (v.IndexOffset + v.IndexNum))
                          ?? AddAccessor(
                              baseName,
                              indexBufferView, GltfBufferViewTarget.ElementArrayBuffer,
                              indexBufferArray, (int) v.IndexOffset, (int) (v.IndexOffset + v.IndexNum)),
                Material = materialIndex,
            })
            .ToList();

        return newPrimitives.Any();
    }

    protected bool CreateModelNode(out GltfNode node, Model xivModel, HkRootLevelContainer? hkSkeletonRoot,
        bool omitSkins = false) {
        var rootNodeName = Path.GetFileNameWithoutExtension(xivModel.File!.FilePath.Path);
        var childPrimitives = new List<GltfMeshPrimitive>();

        List<int> boneIndexToNodeIndex = new();

        var indexBufferOffset = (int) xivModel.File.FileHeader.IndexOffset[(int) xivModel.Lod];
        var indexBufferSize = (int) xivModel.File.FileHeader.IndexBufferSize[(int) xivModel.Lod];
        var indexBufferArray = new ushort[indexBufferSize / 2];
        unsafe {
            fixed (void* src = xivModel.File.Data, dst = indexBufferArray)
                Buffer.MemoryCopy((byte*) src + indexBufferOffset, dst, indexBufferSize, indexBufferSize);
        }

        var indexBufferName = $"{rootNodeName}/indices";
        var indexBufferView =
            GetBufferViewOrDefault(indexBufferName) ??
            AddBufferView(indexBufferName, indexBufferArray, GltfBufferViewTarget.ElementArrayBuffer);

        for (var i = 0; i < xivModel.Meshes.Length; i++) {
            var xivMesh = xivModel.Meshes[i];
            if (!xivMesh.Types.Contains(Mesh.MeshType.Main))
                continue;

            xivMesh.Material.Update(_gameData);
            var materialIndex = WriteMaterial(new(
                _gameData.GetFile<MtrlFile>(xivMesh.Material.ResolvedPath ?? xivMesh.Material.MaterialPath)!));

            if (!WriteMeshOrLogError(out var newPrimitives, $"{rootNodeName}/{i}", xivMesh, indexBufferArray,
                    indexBufferView, materialIndex))
                continue;

            childPrimitives.AddRange(newPrimitives);
        }

        if (!childPrimitives.Any()) {
            Log.D("Model[{0}]: Empty.", rootNodeName);
            node = null!;
            return false;
        }
        Log.D("Model[{0}]: {1} primitive{2}.", rootNodeName, childPrimitives.Count, childPrimitives.Count < 2? "" : "s");

        node = new() {
            Name = rootNodeName,
            Mesh = AddMesh(new() {
                Name = $"{rootNodeName}/mesh",
                Primitives = childPrimitives,
            })
        };

        if (omitSkins)
            Log.D("{0}: Skipping skins.", rootNodeName);
        else if (hkSkeletonRoot != null) {
            if (WriteSkinOrLogError(out var newSkin, boneIndexToNodeIndex, node, hkSkeletonRoot))
                node.Skin = AddSkin(newSkin);
        }
        else
            Log.D("{0}: No skinning info is available.", rootNodeName);

        return true;
    }
}