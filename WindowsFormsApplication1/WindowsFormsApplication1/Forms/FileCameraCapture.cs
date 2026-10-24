using EVotingSystem.Services;

namespace EVotingSystem.Forms;

/// <summary>
/// Default capture source for the desktop build. Uses the built-in Windows webcam
/// (DirectShow/Video-for-Windows) when a device is present; otherwise the operator
/// may select an existing image file. The selected source is always reported
/// honestly in the UI.
/// </summary>
public sealed class CameraCapture : ICameraCapture, IDisposable
{
    private readonly WebcamCapture _webcam = new();

    public bool IsLiveCameraAvailable => _webcam.IsLiveCameraAvailable;

    public string DriverName => _webcam.DriverName;

    public byte[]? Capture()
    {
        if (IsLiveCameraAvailable)
        {
            var frame = _webcam.Capture();
            if (frame != null)
            {
                return frame;
            }
        }

        using var dialog = new OpenFileDialog
        {
            Title = IsLiveCameraAvailable
                ? "Camera capture failed - select an image file instead"
                : "No camera detected - select a captured image file",
            Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp|All files|*.*",
        };

        return dialog.ShowDialog() == DialogResult.OK
            ? File.ReadAllBytes(dialog.FileName)
            : null;
    }

    public void Dispose() => _webcam.Dispose();
}
