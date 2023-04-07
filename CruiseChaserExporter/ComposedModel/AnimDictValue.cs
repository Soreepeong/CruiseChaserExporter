using CruiseChaserExporter.Animation;

namespace CruiseChaserExporter.ComposedModel; 

public class AnimDictValue : Tuple<List<Bone>, Dictionary<string, IAnimation>>{
    public AnimDictValue() : base(new(), new()) { }
    public AnimDictValue(List<Bone> item1, Dictionary<string, IAnimation> item2) : base(item1, item2) { }
}
