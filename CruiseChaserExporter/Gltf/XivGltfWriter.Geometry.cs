using CruiseChaserExporter.Gltf.Models;
using Lumina.Data.Files;
using Lumina.Models.Models;

namespace CruiseChaserExporter.Gltf;

public partial class XivGltfWriter {
    protected bool WritePrimitives(List<GltfMeshPrimitive> childPrimitives, Model xivModel) {
        var rootNodeName = Path.GetFileNameWithoutExtension(xivModel.File!.FilePath.Path);

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

        Dictionary<string, int> matMap = new();

        for (var i = 0; i < xivModel.Meshes.Length; i++) {
            var xivMesh = xivModel.Meshes[i];
            if (!xivMesh.Types.Contains(Mesh.MeshType.Main))
                continue;

            xivMesh.Material.Update(_gameData);
            var matPath = xivMesh.Material.ResolvedPath ?? xivMesh.Material.MaterialPath;
            if (!matMap.TryGetValue(matPath, out var materialIndex))
                materialIndex = matMap[matPath] = WriteMaterial(new(_gameData.GetFile<MtrlFile>(matPath)!));

            GltfMeshPrimitiveAttributes accessors = new();

            var baseName = $"{rootNodeName}/{i}/vertex";
            accessors.Position =
                GetAccessorOrDefault(baseName, 0, xivMesh.Vertices.Length)
                ?? AddAccessor(baseName, -1, GltfBufferViewTarget.ArrayBuffer,
                    xivMesh.Vertices.Select(x => NormalizePosition(x.Position!.Value)).ToArray());

            baseName = $"{rootNodeName}/{i}/normal";
            if (xivMesh.Vertices[0].Normal is not null)
                accessors.Normal =
                    GetAccessorOrDefault(baseName, 0, xivMesh.Vertices.Length)
                    ?? AddAccessor(baseName, -1, GltfBufferViewTarget.ArrayBuffer,
                        xivMesh.Vertices.Select(x => NormalizeNormal(x.Normal!.Value)).ToArray());

            baseName = $"{rootNodeName}/{i}/tangent";
            if (xivMesh.Vertices[0].Tangent1 is not null)
                accessors.Tangent =
                    GetAccessorOrDefault(baseName, 0, xivMesh.Vertices.Length)
                    ?? AddAccessor(baseName, -1, GltfBufferViewTarget.ArrayBuffer,
                        xivMesh.Vertices.Select(x => NormalizeTangent(x.Tangent1!.Value)).ToArray());

            baseName = $"{rootNodeName}/{i}/color";
            if (xivMesh.Vertices[0].Color is not null)
                accessors.Color0 =
                    GetAccessorOrDefault(baseName, 0, xivMesh.Vertices.Length)
                    ?? AddAccessor(baseName, -1, GltfBufferViewTarget.ArrayBuffer,
                        xivMesh.Vertices.Select(x => x.Color!.Value).ToArray());

            baseName = $"{rootNodeName}/{i}/uv";
            if (xivMesh.Vertices[0].UV is not null)
                accessors.TexCoord0 =
                    GetAccessorOrDefault(baseName, 0, xivMesh.Vertices.Length)
                    ?? AddAccessor(baseName, -1, GltfBufferViewTarget.ArrayBuffer,
                        xivMesh.Vertices.Select(x => NormalizeUv(x.UV!.Value)).ToArray());

            if (xivMesh.Vertices[0].BlendWeights is not null) {
                baseName = $"{rootNodeName}/{i}/bone/weights";
                accessors.Weights0 =
                    GetAccessorOrDefault(baseName, 0, xivMesh.Vertices.Length)
                    ?? AddAccessor(baseName, -1, GltfBufferViewTarget.ArrayBuffer,
                        xivMesh.Vertices.Select(x => x.BlendWeights!.Value).ToArray());

                var boneNames = xivMesh.BoneTable
                    .Select(b => xivMesh.Parent.StringOffsetToStringMap[(int) xivMesh.Parent.File!.BoneNameOffsets[b]])
                    .ToArray();

                var indices = xivMesh.Vertices
                    .Select(x => new TypedVec4<ushort>(
                        (ushort) (x.BlendWeights!.Value.X == 0 ? 0 : _boneToBoneIndex[_boneByName[boneNames[x.BlendIndices[0]]]]),
                        (ushort) (x.BlendWeights!.Value.Y == 0 ? 0 : _boneToBoneIndex[_boneByName[boneNames[x.BlendIndices[1]]]]),
                        (ushort) (x.BlendWeights!.Value.Z == 0 ? 0 : _boneToBoneIndex[_boneByName[boneNames[x.BlendIndices[2]]]]),
                        (ushort) (x.BlendWeights!.Value.W == 0 ? 0 : _boneToBoneIndex[_boneByName[boneNames[x.BlendIndices[3]]]])))
                    .ToArray();
                baseName = $"{rootNodeName}/{i}/bone/joints";
                accessors.Joints0 =
                    GetAccessorOrDefault(baseName, 0, xivMesh.Vertices.Length)
                    ?? AddAccessor(baseName, -1, GltfBufferViewTarget.ArrayBuffer, indices);
            }

            baseName = $"{rootNodeName}/{i}/indices";
            if (xivMesh.Submeshes.Any()) {
                childPrimitives.AddRange(xivMesh.Submeshes
                    .Select(v => new GltfMeshPrimitive {
                        Attributes = accessors,
                        Indices = GetAccessorOrDefault(baseName, (int) v.IndexOffset,
                                      (int) (v.IndexOffset + v.IndexNum))
                                  ?? AddAccessor(
                                      baseName,
                                      indexBufferView, GltfBufferViewTarget.ElementArrayBuffer,
                                      indexBufferArray, (int) v.IndexOffset, (int) (v.IndexOffset + v.IndexNum)),
                        Material = materialIndex,
                    }));
            } else {
                childPrimitives.Add(new() {
                    Attributes = accessors,
                    Indices = GetAccessorOrDefault(baseName, 0, xivMesh.Vertices.Length)
                              ?? AddAccessor(
                                  baseName,
                                  indexBufferView, GltfBufferViewTarget.ElementArrayBuffer,
                                  indexBufferArray),
                    Material = materialIndex,
                });
            }
        }

        Log.D("Model[{0}]: {1} {2}.", rootNodeName, childPrimitives.Count,
            childPrimitives.Count < 2 ? "primitive" : "primitives");

        return true;
    }
}
