namespace CruiseChaserExporter.HkDefinitions;

public class HkaAnimationContainer : HkReferencedObject {
    public List<HkaSkeleton> Skeletons;
    public List<HkaAnimation> Animations;
    public List<HkaAnimationBinding> Bindings;
    public List<HkaBoneAttachment> Attachments;
    public List<HkaMeshBinding> Skins;
}