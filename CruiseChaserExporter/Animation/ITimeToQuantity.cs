namespace CruiseChaserExporter.Animation; 

public interface ITimeToQuantity {
    bool IsEmpty { get; }
    
    bool IsStatic { get; }
    
    float Duration { get; }

    /// <summary>
    /// Get the times of "keyframes." Includes the duration itself.
    /// </summary>
    IEnumerable<float> GetFrameTimes();
}
