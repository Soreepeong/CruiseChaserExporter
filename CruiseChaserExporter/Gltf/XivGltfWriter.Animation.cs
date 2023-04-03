using System.Numerics;
using CruiseChaserExporter.Gltf.Models;
using CruiseChaserExporter.HkAnimationStuff;
using CruiseChaserExporter.HkDefinitions;

namespace CruiseChaserExporter.Gltf;

public partial class XivGltfWriter {
    private static void ExtendAnimationTrack<T>(
        IReadOnlyList<T> inValues,
        List<T> outValues,
        List<float> outTimes,
        HkAnimationDecoder anim) {
        if (inValues.Count == 0)
            return;
        
        var timeBase = 0f;
        if (outTimes.Any()) {
            timeBase = outTimes[^1];
            outTimes.RemoveAt(outTimes.Count - 1);
            outValues.RemoveAt(outValues.Count - 1);
        }
                    
        if (inValues.Count == 1) {
            outTimes.Add(timeBase);
            outTimes.Add(Math.Min(anim.Duration, timeBase + anim.BlockDuration));
            outValues.Add(inValues[0]);
            outValues.Add(inValues[0]);
        } else {
            outTimes.AddRange(Enumerable.Range(0, inValues.Count).Select(x => timeBase + x * anim.FrameDuration));
            outValues.AddRange(inValues);
        }
    }

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
    
    private int WriteAnimations(
        string animName,
        HkRootLevelContainer animationContainerUntyped,
        IList<int> boneIndexToNodeIndex) {
        if (!animationContainerUntyped.NamedVariants.Any())
            return 0;
        if (animationContainerUntyped.NamedVariants[0].Variant is not HkaAnimationContainer animationContainer)
            return 0;

        var numAnimationsWritten = 0;
        var animCount = Math.Min(animationContainer.Animations.Length, animationContainer.Bindings.Length);
        for (var animIndex = 0; animIndex < animCount; animIndex++) {
            var anim = HkAnimationDecoder.Decode(animationContainer.Animations[animIndex]);
            var trackIndexToBoneIndex = animationContainer.Bindings[animIndex].TransformTrackToBoneIndices;

            var trackCount = trackIndexToBoneIndex.Length;
            var translateTimes = Enumerable.Range(0, trackCount).Select(_ => new List<float>()).ToArray();
            var translates = Enumerable.Range(0, trackCount).Select(_ => new List<Vector3>()).ToArray();
            var rotateTimes = Enumerable.Range(0, trackCount).Select(_ => new List<float>()).ToArray();
            var rotates = Enumerable.Range(0, trackCount).Select(_ => new List<Quaternion>()).ToArray();
            var scaleTimes = Enumerable.Range(0, trackCount).Select(_ => new List<float>()).ToArray();
            var scales = Enumerable.Range(0, trackCount).Select(_ => new List<Vector3>()).ToArray();

            foreach (var tracks in anim.TrackBlocks) {
                for (var trackIndex = 0; trackIndex < tracks.Count; trackIndex++) {
                    var trs = tracks[trackIndex];
                    ExtendAnimationTrack(trs.Translate, translates[trackIndex], translateTimes[trackIndex], anim);
                    ExtendAnimationTrack(trs.Rotate, rotates[trackIndex], rotateTimes[trackIndex], anim);
                    ExtendAnimationTrack(trs.Scale, scales[trackIndex], scaleTimes[trackIndex], anim);
                }
            }

            var newAnimation = new GltfAnimation {
                Name = $"{animName}/{animIndex}",
            };

            for (var trackIndex = 0; trackIndex < trackCount; trackIndex++) {
                var boneNodeIndex = boneIndexToNodeIndex[trackIndexToBoneIndex[trackIndex]];
                
                AddAnimationComponent(
                    newAnimation,
                    GltfAnimationChannelTargetPath.Translation,
                    boneNodeIndex,
                    translates[trackIndex],
                    translateTimes[trackIndex]);

                AddAnimationComponent(
                    newAnimation,
                    GltfAnimationChannelTargetPath.Rotation,
                    boneNodeIndex,
                    rotates[trackIndex],
                    rotateTimes[trackIndex]);

                AddAnimationComponent(
                    newAnimation,
                    GltfAnimationChannelTargetPath.Scale,
                    boneNodeIndex,
                    scales[trackIndex],
                    scaleTimes[trackIndex]);
            }

            if (!newAnimation.Channels.Any() || !newAnimation.Samplers.Any())
                continue;

            AddAnimation(newAnimation);
            numAnimationsWritten++;
        }

        return numAnimationsWritten;
    }
}
