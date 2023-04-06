using System.Numerics;
using CruiseChaserExporter.Util;
using Lumina.Data;
using Lumina.Extensions;

namespace CruiseChaserExporter.XivStruct;

public class PbdFile : FileResource {
    public HeaderBySkeletonId[] HeadersBySkeleton = null!;
    public HeaderByDeformerId[] HeadersByDeformer = null!;
    public Deformer[] Deformers = null!;

    public override void LoadFile() {
        var entryCount = Reader.ReadInt32();

        HeadersBySkeleton = Reader.ReadStructuresAsArray<HeaderBySkeletonId>(entryCount);
        HeadersByDeformer = Reader.ReadStructuresAsArray<HeaderByDeformerId>(entryCount);
        Deformers = HeadersBySkeleton.Where(x => x.Offset != 0)
            .Select(x => new Deformer(Reader.SeekThen(x.Offset, SeekOrigin.Begin)))
            .ToArray();
    }

    public bool TryGetDeformerBySkeletonId(int skeletonId, out Deformer deformer) {
        foreach (var (s, d) in HeadersBySkeleton.Zip(Deformers)) {
            if (s.SkeletonId == skeletonId) {
                deformer = d;
                return true;
            }
        }

        deformer = new();
        return false;
    }

    public struct HeaderByDeformerId {
        public ushort ParentDeformerIndex;
        public ushort Unknown2;
        public ushort Unknown3;
        public ushort SkeletonIndex;

        public override string ToString() => $"{ParentDeformerIndex}, {Unknown2:X04}, {Unknown3:X04}, {SkeletonIndex}";
    }

    public struct HeaderBySkeletonId {
        public ushort SkeletonId;
        public ushort DeformerId;
        public int Offset;
        public float BaseScale;

        public override string ToString() => $"{SkeletonId}, {DeformerId}, {BaseScale:0.000}";
    }

    public struct Deformer {
        public int BoneCount = 0;
        public string[] BoneNames = Array.Empty<string>();
        public Matrix4x4[] Matrices = Array.Empty<Matrix4x4>();
        public Vector3[] Translations = Array.Empty<Vector3>();
        public Quaternion[] Rotations = Array.Empty<Quaternion>();
        public Vector3[] Scales = Array.Empty<Vector3>();

        public Deformer() { }

        public Deformer(BinaryReader reader) {
            var baseOffset = reader.BaseStream.Position;
            reader.ReadInto(out BoneCount);

            var nameOffsets = reader.ReadStructuresAsArray<ushort>(BoneCount);
            reader.AlignTo(4);

            Translations = new Vector3[BoneCount];
            Rotations = new Quaternion[BoneCount];
            Scales = new Vector3[BoneCount];
            Matrices = new Matrix4x4[BoneCount];
            for (var i = 0; i < BoneCount; i++) {
                Matrices[i] = new(
                    reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                    reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                    reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                    0, 0, 0, 1
                );
                
                var transposed = Matrix4x4.Transpose(Matrices[i]);
                if (!Matrix4x4.Decompose(transposed, out Scales[i], out Rotations[i], out Translations[i])) {
                    // s @ r @ t = m
                    // s @ r = m @ t^-1
                    // r = s^-1 @ m @ t^-1
                    if (!Matrix4x4.Invert(Matrix4x4.CreateTranslation(Translations[i]), out var invTranslation))
                        throw new InvalidOperationException();
                    if (!Matrix4x4.Invert(Matrix4x4.CreateScale(Scales[i]), out var invScale))
                        throw new InvalidOperationException();
                    Rotations[i] = Quaternion.CreateFromRotationMatrix(invScale * transposed * invTranslation);
                }
            }

            BoneNames = nameOffsets
                .Select(x => reader.SeekThen(baseOffset + x, SeekOrigin.Begin).ReadCString())
                .ToArray();
        }
    }
}
