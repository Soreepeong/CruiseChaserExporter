using System.Numerics;
using CruiseChaserExporter.Gltf.Models;
using CruiseChaserExporter.HkAnimationStuff;
using CruiseChaserExporter.HkDefinitions;

namespace CruiseChaserExporter.Gltf;

public partial class XivGltfWriter {
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

            for (var animSubIndex = 0; animSubIndex < anim.Tracks.Count; animSubIndex++) {
                var tracks = anim.Tracks[animSubIndex];
                var newAnimation = new GltfAnimation {
                    Name = $"{animName}/{animIndex}/{animSubIndex}",
                };

                for (var trackIndex = 0; trackIndex < tracks.Count; trackIndex++) {
                    var boneIndex = trackIndexToBoneIndex[trackIndex];
                    var trs = tracks[trackIndex];

                    float[]? times;
                    Vector3[]? translates;
                    Quaternion[]? rotates;
                    Vector3[]? scales;

                    switch (trs.Translate.Length) {
                        case 1:
                            times = new[] {0, anim.Duration};
                            translates = new[] {trs.Translate[0], trs.Translate[0]};
                            break;
                        case > 1:
                            times = Enumerable.Range(0, trs.Translate.Length).Select(x => x * anim.FrameDuration)
                                .ToArray();
                            translates = trs.Translate;
                            break;
                        default:
                            times = null;
                            translates = null;
                            break;
                    }

                    if (translates is not null && times is not null) {
                        newAnimation.Samplers.Add(new() {
                            Input = AddAccessor(
                                $"animation/{animName}/{animIndex}/{animSubIndex}/translate/time", -1, null, times),
                            Output = AddAccessor(
                                $"animation/{animName}/{animIndex}/{animSubIndex}/translate/vec3", -1, null,
                                translates),
                            Interpolation = GltfAnimationSamplerInterpolation.Linear,
                        });
                        newAnimation.Channels.Add(new() {
                            Sampler = newAnimation.Samplers.Count - 1,
                            Target = new() {
                                Node = boneIndexToNodeIndex[boneIndex],
                                Path = GltfAnimationChannelTargetPath.Translation,
                            },
                        });
                    }

                    switch (trs.Rotate.Length) {
                        case 1:
                            times = new[] {0, anim.Duration};
                            rotates = new[] {trs.Rotate[0], trs.Rotate[0]};
                            break;
                        case > 1:
                            times = Enumerable.Range(0, trs.Rotate.Length).Select(x => x * anim.FrameDuration)
                                .ToArray();
                            rotates = trs.Rotate;
                            break;
                        default:
                            times = null;
                            rotates = null;
                            break;
                    }

                    if (rotates is not null && times is not null) {
                        newAnimation.Samplers.Add(new() {
                            Input = AddAccessor(
                                $"animation/{animName}/{animIndex}/{animSubIndex}/rotate/time", -1, null, times),
                            Output = AddAccessor(
                                $"animation/{animName}/{animIndex}/{animSubIndex}/rotate/quat", -1, null, rotates),
                            Interpolation = GltfAnimationSamplerInterpolation.Linear,
                        });
                        newAnimation.Channels.Add(new() {
                            Sampler = newAnimation.Samplers.Count - 1,
                            Target = new() {
                                Node = boneIndexToNodeIndex[boneIndex],
                                Path = GltfAnimationChannelTargetPath.Rotation,
                            },
                        });
                    }

                    switch (trs.Scale.Length) {
                        case 1:
                            times = new[] {0, anim.Duration};
                            scales = new[] {trs.Scale[0], trs.Scale[0]};
                            break;
                        case > 1:
                            times = Enumerable.Range(0, trs.Scale.Length).Select(x => x * anim.FrameDuration).ToArray();
                            scales = trs.Scale;
                            break;
                        default:
                            times = null;
                            scales = null;
                            break;
                    }

                    if (scales is not null && times is not null) {
                        newAnimation.Samplers.Add(new() {
                            Input = AddAccessor(
                                $"animation/{animName}/{animIndex}/{animSubIndex}/scale/time", -1, null, times),
                            Output = AddAccessor(
                                $"animation/{animName}/{animIndex}/{animSubIndex}/scale/vec3", -1, null, scales),
                            Interpolation = GltfAnimationSamplerInterpolation.Linear,
                        });
                        newAnimation.Channels.Add(new() {
                            Sampler = newAnimation.Samplers.Count - 1,
                            Target = new() {
                                Node = boneIndexToNodeIndex[boneIndex],
                                Path = GltfAnimationChannelTargetPath.Scale,
                            },
                        });
                    }
                }

                if (!newAnimation.Channels.Any() || !newAnimation.Samplers.Any())
                    continue;

                AddAnimation(newAnimation);
                numAnimationsWritten++;
            }
        }

        return numAnimationsWritten;
    }
}
