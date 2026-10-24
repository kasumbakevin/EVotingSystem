using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using EVotingSystem.Services;

namespace EVotingSystem.Forms;

/// <summary>
/// Live webcam still capture using the built-in Windows Video-for-Windows (avicap32)
/// API. No third-party library is required. If no capture device is present the
/// capture operation reports failure and the UI falls back to selecting an image.
/// </summary>
public sealed class WebcamCapture : ICameraCapture, IDisposable
{
    private IntPtr _captureWindow = IntPtr.Zero;
    private string? _tempFile;

    public bool IsLiveCameraAvailable
    {
        get
        {
            if (!OperatingSystem.IsWindows())
            {
                return false;
            }

            return NativeMethods.capGetDriverDescription(0, null!, 0, null!, 0) > 0;
        }
    }

    public string DriverName
    {
        get
        {
            var buffer = new System.Text.StringBuilder(256);
            var ok = NativeMethods.capGetDriverDescription(0, buffer, buffer.Capacity, null!, 0);
            return ok > 0 ? buffer.ToString() : string.Empty;
        }
    }

    public byte[]? Capture()
    {
        if (!IsLiveCameraAvailable)
        {
            return null;
        }

        try
        {
            EnsureConnected();

            _tempFile = Path.Combine(Path.GetTempPath(), $"evoting_capture_{Guid.NewGuid():N}.bmp");
            NativeMethods.capGrabFrame(_captureWindow);
            NativeMethods.capEditCopy(_captureWindow);
            var saved = NativeMethods.capFileSaveDIB(_captureWindow, _tempFile);
            if (saved != 1 || !File.Exists(_tempFile))
            {
                return null;
            }

            using var bitmap = new Bitmap(_tempFile);
            using var png = new MemoryStream();
            bitmap.Save(png, ImageFormat.Png);
            return png.ToArray();
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            TryDeleteTemp();
        }
    }

    private void EnsureConnected()
    {
        if (_captureWindow != IntPtr.Zero)
        {
            return;
        }

        var host = new NativeWindow();
        _captureWindow = NativeMethods.capCreateCaptureWindow(
            "EVotingCamera",
            0,
            0,
            0,
            320,
            240,
            host.Handle,
            0);

        if (_captureWindow == IntPtr.Zero)
        {
            throw new InvalidOperationException("Unable to create the camera capture window.");
        }

        if (!NativeMethods.capDriverConnect(_captureWindow, 0))
        {
            NativeMethods.DestroyWindow(_captureWindow);
            _captureWindow = IntPtr.Zero;
            throw new InvalidOperationException("No camera driver could be connected.");
        }

        _host = host;
    }

    private NativeWindow? _host;

    private void TryDeleteTemp()
    {
        if (_tempFile == null)
        {
            return;
        }

        try
        {
            if (File.Exists(_tempFile))
            {
                File.Delete(_tempFile);
            }
        }
        catch (IOException)
        {
        }
        finally
        {
            _tempFile = null;
        }
    }

    public void Dispose()
    {
        if (_captureWindow != IntPtr.Zero)
        {
            NativeMethods.capDriverDisconnect(_captureWindow);
            NativeMethods.DestroyWindow(_captureWindow);
            _captureWindow = IntPtr.Zero;
        }

        _host?.DestroyHandle();
        _host = null;
    }

    private static class NativeMethods
    {
        [DllImport("avicap32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        internal static extern IntPtr capCreateCaptureWindow(
            string lpszWindowName,
            int dwStyle,
            int x,
            int y,
            int nWidth,
            int nHeight,
            IntPtr hWndParent,
            int nID);

        [DllImport("avicap32.dll", CharSet = CharSet.Ansi)]
        internal static extern int capGetDriverDescription(
            int wDriverIndex,
            System.Text.StringBuilder lpszName,
            int cbName,
            System.Text.StringBuilder lpszVer,
            int cbVer);

        [DllImport("avicap32.dll")]
        internal static extern bool capDriverConnect(IntPtr hWnd, int i);

        [DllImport("avicap32.dll")]
        internal static extern bool capDriverDisconnect(IntPtr hWnd);

        [DllImport("avicap32.dll")]
        internal static extern bool capGrabFrame(IntPtr hWnd);

        [DllImport("avicap32.dll", EntryPoint = "capEditCopy")]
        internal static extern bool capEditCopy(IntPtr hWnd);

        [DllImport("avicap32.dll", CharSet = CharSet.Ansi)]
        internal static extern int capFileSaveDIB(IntPtr hWnd, string szName);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool DestroyWindow(IntPtr hWnd);
    }
}
