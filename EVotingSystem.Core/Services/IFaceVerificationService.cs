namespace EVotingSystem.Services
{
    public enum FaceVerificationOutcome
    {
        Verified = 0,
        RetryRequired = 1,
        ManualReview = 2,
        Failed = 3,
        Unavailable = 4,
    }

    public sealed record FaceVerificationResult(
        FaceVerificationOutcome Outcome,
        string Message,
        double? MatchScore = null);

    /// <summary>
    /// Contract for optional facial identity verification. A face match alone must
    /// never be treated as proof of voter eligibility.
    ///
    /// No genuine implementation ships with this build. Install a suitable
    /// face-verification library or a secure external provider and register it here;
    /// until then <see cref="UnavailableFaceVerificationService"/> is used and the
    /// feature is reported as unavailable rather than faked.
    /// </summary>
    public interface IFaceVerificationService
    {
        bool IsAvailable { get; }

        FaceVerificationResult Verify(byte[] referenceImage, byte[] capturedImage);
    }

    /// <summary>
    /// Placeholder used when no face-verification provider is configured. Always
    /// returns <see cref="FaceVerificationOutcome.Unavailable"/> so callers can fall
    /// back to an authorized manual verification workflow.
    /// </summary>
    public sealed class UnavailableFaceVerificationService : IFaceVerificationService
    {
        public bool IsAvailable => false;

        public FaceVerificationResult Verify(byte[] referenceImage, byte[] capturedImage)
            => new(
                FaceVerificationOutcome.Unavailable,
                "Facial verification is not configured. Use manual verification by an authorized officer.");
    }
}
