using CruiseChaserExporter.Animation;
using CruiseChaserExporter.Gltf.Models;

namespace CruiseChaserExporter.Gltf;

public partial class XivGltfWriter {
    private void AddAnimationComponent<T>(
        GltfAnimation target,
        GltfAnimationChannelTargetPath purpose,
        int boneNodeIndex,
        IEnumerable<T> values,
        IEnumerable<float> times) where T : unmanaged {
        var valueArray = values.ToArray();
        if (!valueArray.Any())
            return;

        var timesArray = times.ToArray();

        target.Samplers.Add(new() {
            Input = AddAccessor($"anim/{target.Name}/{boneNodeIndex}/tt", -1, null, timesArray),
            Output = AddAccessor($"anim/{target.Name}/{boneNodeIndex}/tv", -1, null, valueArray),
            Interpolation = GltfAnimationSamplerInterpolation.Linear,
        });
        target.Channels.Add(new() {
            Sampler = target.Samplers.Count - 1,
            Target = new() {
                Node = boneNodeIndex,
                Path = purpose,
            },
        });
    }

    private bool WriteAnimation(
        string animName,
        IAnimation anim,
        IList<int> boneIndexToNodeIndex) {
        var target = new GltfAnimation {
            Name = $"{animName}",
        };

        foreach (var bone in anim.AffectedBoneIndices) {
            var node = boneIndexToNodeIndex[bone];

            var translation = anim.Translation(bone);
            if (!translation.IsEmpty) {
                var times = translation.GetFrameTimes().Append(anim.Duration).ToArray();
                var values = times.Select(x => translation.Interpolate(x)).ToArray();

                AddAnimationComponent(target, GltfAnimationChannelTargetPath.Translation, node, values, times);
            }

            var rotation = anim.Rotation(bone);
            if (!rotation.IsEmpty) {
                var times = rotation.GetFrameTimes().Append(anim.Duration).ToArray();
                var values = times.Select(x => rotation.Interpolate(x)).ToArray();

                AddAnimationComponent(target, GltfAnimationChannelTargetPath.Rotation, node, values, times);
            }

            var scale = anim.Scale(bone);
            if (!scale.IsEmpty) {
                var times = scale.GetFrameTimes().Append(anim.Duration).ToArray();
                var values = times.Select(x => scale.Interpolate(x)).ToArray();

                AddAnimationComponent(target, GltfAnimationChannelTargetPath.Scale, node, values, times);
            }
        }

        if (!target.Channels.Any() || !target.Samplers.Any())
            return false;

        AddAnimation(target);
        return true;
    }
}
