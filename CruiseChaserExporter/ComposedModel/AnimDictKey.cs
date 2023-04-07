using CruiseChaserExporter.XivStruct;

namespace CruiseChaserExporter.ComposedModel;

public class AnimDictKey : Tuple<PapFile.PapTargetModelType, ushort> {
    public AnimDictKey(PapFile.PapTargetModelType item1, ushort item2) : 
        base(item1, item1 == PapFile.PapTargetModelType.Invalid ? (ushort) 0xFFFF : item2) { }

    public AnimDictKey(PapFile.PapTargetModelType item1, XivHumanSkeletonId item2) :
        this(item1, (ushort) item2) { }

    public AnimDictKey(PapFile papFile) :
        this(papFile.Header.ModelType, papFile.Header.ModelId) { }

    public static readonly AnimDictKey Invalid = new(PapFile.PapTargetModelType.Invalid, (ushort)0);
}
