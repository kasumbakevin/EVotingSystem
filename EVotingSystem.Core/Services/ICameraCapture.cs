namespace EVotingSystem.Services
{
    /// <summary>
    /// Abstraction for capturing a still image for identity verification.
    ///
    /// A live-webcam implementation requires a capture library (e.g. a DirectShow
    /// or Media Foundation wrapper) that is not referenced by this build. Until one
    /// is installed and registered here, the file-based implementation is used and
    /// is clearly labelled as such in the UI — it is never presented as a live
    /// camera capture.
    /// </summary>
    public interface ICameraCapture
    {
        bool IsLiveCameraAvailable { get; }

        /// <summary>Capture a still image and return it as PNG-encoded bytes, or null.</summary>
        byte[]? Capture();
    }
}
