using System.Collections.Immutable;
using System.Numerics;
using CruiseChaserExporter.Animation;
using CruiseChaserExporter.Gltf;
using CruiseChaserExporter.HavokCodec;
using CruiseChaserExporter.HavokCodec.AnimationCodec;
using CruiseChaserExporter.HavokCodec.HavokTagfile;
using CruiseChaserExporter.HavokCodec.KnownDefinitions;
using CruiseChaserExporter.XivStruct;
using Lumina;
using Lumina.Models.Models;

namespace CruiseChaserExporter.ComposedModel;

public class ModelExporter {
    private readonly Dictionary<Tuple<string, int>, Definition> _havokDefinitions = new();
    private readonly GameData _gameData;

    private readonly List<SourceModel> _sourceItems = new();

    private readonly Dictionary<
        Tuple<PapFile.PapTargetModelType, int>,
        Tuple<List<Bone>, Dictionary<string, IAnimation>>
    > _animations =
        new();

    private readonly int? _baseRaceId;
    private readonly List<Bone>? _baseBone;

    public ModelExporter(GameData gameData, int? baseRaceId = null) {
        _gameData = gameData;
        if (baseRaceId is not null) {
            _baseBone = _GetBones(string.Format(
                "chara/human/c{0:D4}/skeleton/base/b0001/skl_c{0:D4}b0001.sklb", baseRaceId));
            if (_baseBone is not null)
                _baseRaceId = baseRaceId;
        }
    }

    public ModelExporter AddAnimation(string papPath) {
        var papFile = _gameData.GetFile<PapFile>(papPath)!;
        var rootUntyped = Parser.Parse(papFile.HavokData, _havokDefinitions);
        var root = new TypedHavokDeserializer(typeof(HkRootLevelContainer), _havokDefinitions)
            .Deserialize<HkRootLevelContainer>(rootUntyped);
        if (root.NamedVariants.FirstOrDefault()?.Variant is not HkaAnimationContainer animationContainer)
            throw new InvalidDataException("Given file does not have a hkaAnimationContainer.");

        var modelKey = Tuple.Create(papFile.Header.ModelType, (int) papFile.Header.ModelId);
        if (!_animations.TryGetValue(modelKey, out var animations))
            _animations[modelKey] = animations = new(new(), new());
        for (var i = 0; i < animationContainer.Bindings.Length; i++)
            animations.Item2[$"{papPath}:{i}"] = AnimationSet.Decode(animationContainer.Bindings[i]);
        return this;
    }

    public ModelExporter AddModel(string mdlPath, string? sklbPath = null, int variantId = 1) {
        var pathComponents = mdlPath.Split('/');
        if (pathComponents[0] != "chara" || pathComponents.Length < 2)
            throw new ArgumentException(null, nameof(mdlPath));

        var model = new Model(_gameData, mdlPath, Model.ModelLod.High, variantId);
        sklbPath ??= _FindSklbPath(pathComponents);
        var bones = _GetBones(sklbPath);

        var res = new SourceModel(model, bones, GetAnimationSkeletonKey(sklbPath?.Split('/')));
        _sourceItems.Add(res);
        return this;
    }

    public ModelExporter AddMonster(int monsterId, int bodyId, int variantId) =>
        AddModel(string.Format(
            "chara/monster/m{0:D4}/obj/body/b{1:D4}/model/m{0:D4}b{1:D4}.mdl",
            monsterId, bodyId), null, variantId);

    public XivGltfWriter Export() {
        var bones = new List<Bone>();
        var rootBones = new Dictionary<Bone, HashSet<Tuple<PapFile.PapTargetModelType, int>>>();
        if (_baseBone != null && _baseRaceId != null) {
            bones.AddRange(_baseBone);
            rootBones[_baseBone.Single(x => x.Parent is null)] =
                new() {Tuple.Create(PapFile.PapTargetModelType.Human, _baseRaceId.Value)};
        }

        foreach (var model in _sourceItems) {
            if (model.Bones is null && model.Model.Meshes.Any(x => x.BoneTable.Any()))
                model.Bones = _baseBone;

            if (model.RootBone is null || model.Bones is null)
                continue;

            var bone = bones.SingleOrDefault(x => x.Name == model.RootBone.Name);
            if (bone is null) {
                HashSet<Tuple<PapFile.PapTargetModelType, int>>? animKeys = null;
                foreach (var (newParent, unroot) in model.Bones
                             .Select(x => (x, rootBones.Keys.SingleOrDefault(y => x.Name == y.Name)))) {
                    if (unroot is null)
                        continue;
                    if (!rootBones.Remove(unroot, out animKeys))
                        throw new InvalidOperationException();
                    foreach (var child in unroot.Children)
                        child.Parent = newParent;
                    foreach (var reroot in _sourceItems.Where(x => x.RootBone == unroot))
                        reroot.RootBone = newParent;
                }

                animKeys ??= new();
                animKeys.Add(model.AnimationKey);

                bones.AddRange(model.Bones);
                rootBones.Add(model.RootBone, animKeys);
            } else {
                model.RootBone = bone;
            }
        }

        var boneByName = bones.ToDictionary(x => x.Name, x => x); 

        var writer = new XivGltfWriter("GLTF", false, true, _gameData);
        var rootBoneCounter = 0;
        foreach (var (rootBone, animKeys) in rootBones) {
            writer.AddSkin($"Root#{rootBoneCounter++}", rootBone);

            var pbdFile = _gameData.GetFile<PbdFile>("chara/xls/boneDeformer/human.pbd")!;
            if (pbdFile.TryGetDeformerBySkeletonId(_baseRaceId ?? 0xFFFF, out var deformer)) {
                var translations = deformer.Translations.Select((x, i) => (x, i)).ToDictionary(x => x.i, x => x.x);
                var rotations = deformer.Rotations.Select((x, i) => (x, i)).ToDictionary(x => x.i, x => Quaternion.Normalize(x.x));
                var scales = deformer.Scales.Select((x, i) => (x, i)).ToDictionary(x => x.i, x => x.x);

                writer.AddAnimation("deformation", new StaticAnimation(translations, rotations, scales),
                    deformer.BoneNames.Select(x => boneByName[x]));
            }

            foreach (var animKey in animKeys) {
                if (!_animations.TryGetValue(animKey, out var animTuple))
                    continue;

                foreach (var (path, animation) in animTuple.Item2)
                    writer.AddAnimation(path, animation, animTuple.Item1);
            }
        }

        foreach (var model in _sourceItems) {
            if (!writer.AddModel(Path.GetFileNameWithoutExtension(model.Model.File!.FilePath), model))
                throw new InvalidOperationException();
        }

        return writer;
    }

    private List<Bone>? _GetBones(string? sklbPath) {
        if (sklbPath is null)
            return null;

        var sklbFile = _gameData.GetFile<SklbFile>(sklbPath);
        if (sklbFile is null)
            return null;

        var rootUntyped = Parser.Parse(sklbFile.HavokData, _havokDefinitions);
        var root = new TypedHavokDeserializer(typeof(HkRootLevelContainer), _havokDefinitions)
            .Deserialize<HkRootLevelContainer>(rootUntyped);
        if (root.NamedVariants.SingleOrDefault(x => x.Variant is HkaAnimationContainer)?.Variant is not HkaAnimationContainer animationContainer)
            return null;

        if (animationContainer.Skeletons.FirstOrDefault() is not { } hkaSkeleton)
            return null;

        var bones = new List<Bone>();
        foreach (var (bone, parentIndex, pose)
                 in hkaSkeleton.Bones.Zip(hkaSkeleton.ParentIndices, hkaSkeleton.ReferencePose
                 )) {
            bones.Add(new(
                parentIndex == -1 ? null : bones[parentIndex],
                bone.Name ?? $"<unnamed #{bones.Count}>",
                new(pose[0], pose[1], pose[2]),
                new(pose[4], pose[5], pose[6], pose[7]),
                new(pose[8], pose[9], pose[10])
            ));
        }

        var animKey = GetAnimationSkeletonKey(sklbPath.Split("/"));
        if (animKey.Item1 == PapFile.PapTargetModelType.Invalid)
            return bones.ToList();
        
        if (!_animations.TryGetValue(animKey, out var animTuple))
            _animations[animKey] = animTuple = new(new(), new());
        animTuple.Item1.Clear();
        animTuple.Item1.AddRange(bones);

        return animTuple.Item1;
    }

    private string? _FindSklbPath(IReadOnlyList<string> pathComponents) {
        switch (pathComponents[1]) {
            case "human":
                // chara/human/c????/*/<part>/.????/*/...
                return _FindSklbPathImplHuman(
                    part: pathComponents[4],
                    raceId: Convert.ToInt32(pathComponents[2][1..], 10),
                    setId: Convert.ToInt32(pathComponents[5][1..], 10));
            case "equipment":
                // chara/equipment/e????/model/c????e????_<part>.mdl
                return _FindSklbPathImplHuman(
                    part: Path.GetFileNameWithoutExtension(pathComponents[^1]).Split('_', 2)[1],
                    raceId: Convert.ToInt32(pathComponents[^1][1..5], 10),
                    setId: Convert.ToInt32(pathComponents[^1][6..10], 10));
            case "demihuman":
            case "monster":
            case "weapon":
                return string.Format(
                    "chara/{0}/{1}{2:D4}/skeleton/base/b0001/skl_{1}{2:D4}b0001.sklb",
                    pathComponents[1], pathComponents[1][0], int.Parse(pathComponents[2][1..])
                );
            default:
                return null;
        }
    }

    private string? _FindSklbPathImplHuman(string part, int raceId, int setId) {
        if (part == "body") {
            // use default
            return null;
        }

        var estPath = part switch {
            "face" => "chara/xls/charadb/faceSkeletonTemplate.est",
            "hair" => "chara/xls/charadb/hairSkeletonTemplate.est",
            "met" => "chara/xls/charadb/extra_met.est",
            "top" => "chara/xls/charadb/extra_top.est",
            _ => null,
        };
        if (estPath == null)
            return null;

        var skeletonId = _gameData.GetFile<EstFile>(estPath)!.GetSkeletonId(raceId, setId);
        return string.Format(
            "chara/human/c{0:D4}/skeleton/{1}/{2}{3:D4}/skl_c{0:D4}{2}{3:D4}.sklb",
            raceId, part, part[0], skeletonId
        );
    }

    private static Tuple<PapFile.PapTargetModelType, int>
        GetAnimationSkeletonKey(IReadOnlyList<string>? pathComponents) {
        if (pathComponents is null)
            return Tuple.Create(PapFile.PapTargetModelType.Invalid, -1);
        var targetModelType = pathComponents[1] switch {
                "human" when pathComponents[4] == "base" => PapFile.PapTargetModelType.Human,
                "monster" => PapFile.PapTargetModelType.Monster,
                "demihuman" => PapFile.PapTargetModelType.DemiHuman,
                "weapon" => PapFile.PapTargetModelType.Weapon,
                _ => PapFile.PapTargetModelType.Invalid,
            };
        return Tuple.Create(targetModelType, targetModelType == PapFile.PapTargetModelType.Invalid
            ? -1
            : Convert.ToInt32(pathComponents[2][1..], 10));
    }
}
