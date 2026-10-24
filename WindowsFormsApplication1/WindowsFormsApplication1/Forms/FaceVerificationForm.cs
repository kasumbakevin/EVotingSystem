using EVotingSystem.Data;
using EVotingSystem.Models;
using EVotingSystem.Services;

namespace EVotingSystem.Forms;

/// <summary>
/// Identity verification screen: capture a reference photo, capture a live/scan
/// image, run optional facial matching, and record the outcome. Where no
/// face-verification provider is configured an authorized officer can complete a
/// manual review. A face match alone is never treated as proof of eligibility.
/// </summary>
public class FaceVerificationForm : Form, INavigationAware
{
    private readonly Voter _voter;
    private readonly VoterService _voterService;
    private readonly PhotoStore _photoStore;
    private readonly IFaceVerificationService _faceService;
    private readonly ICameraCapture _camera;
    private readonly CameraCapture? _disposableCamera;
    private readonly FormNavigationService _navigation;

    private readonly PictureBox _referencePreview = new();
    private readonly PictureBox _capturePreview = new();
    private readonly Label _statusLabel = new();
    private readonly Label _providerLabel = new();

    private byte[]? _referenceBytes;
    private byte[]? _capturedBytes;

    public FaceVerificationForm(
        Voter voter,
        DatabaseFactory databaseFactory,
        FormNavigationService navigation)
    {
        _voter = voter;
        _voterService = new VoterService(databaseFactory);
        _photoStore = new PhotoStore();
        _faceService = new UnavailableFaceVerificationService();
        _disposableCamera = new CameraCapture();
        _camera = _disposableCamera;
        _navigation = navigation;

        Text = $"Identity Verification - {voter.Name}";
        Size = new Size(820, 640);
        MinimumSize = new Size(760, 580);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        BackColor = UiTheme.Canvas;

        BuildInterface();
        LoadExistingReference();
        FormClosed += (_, _) => _disposableCamera.Dispose();
    }

    public void RequestBack() => _navigation.Back();

    private void BuildInterface()
    {
        var header = new GradientPanel
        {
            Dock = DockStyle.Top,
            Height = 96,
            Padding = new Padding(24, 12, 24, 10),
            StartColor = UiTheme.HeaderStart,
            EndColor = UiTheme.HeaderEnd,
        };
        var title = new Label
        {
            Text = "IDENTITY VERIFICATION",
            Dock = DockStyle.Top,
            Height = 36,
            Font = UiTheme.CreateFont(18f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        var subtitle = new Label
        {
            Text = "Capture a reference photo, scan, and record the verification outcome",
            Dock = DockStyle.Fill,
            Font = UiTheme.CreateFont(9.5f),
            ForeColor = UiTheme.HeaderSubtext,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        header.Controls.Add(subtitle);
        header.Controls.Add(title);

        var previews = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 280,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(20, 14, 20, 8),
            BackColor = UiTheme.Canvas,
        };
        previews.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        previews.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        previews.Controls.Add(BuildPreviewCard("REFERENCE PHOTO", _referencePreview), 0, 0);
        previews.Controls.Add(BuildPreviewCard("CAPTURED / SCAN", _capturePreview), 1, 0);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 56,
            BackColor = UiTheme.Canvas,
            Padding = new Padding(20, 6, 20, 4),
        };
        AddAction(actions, "\uE722", "Capture Reference", (_, _) => CaptureReference(), UiTheme.Green);
        AddAction(actions, "\uE8B7", "Scan Face", (_, _) => ScanFace(), UiTheme.Ocean);
        AddAction(actions, "\uE72C", "Retake", (_, _) => Retake(), UiTheme.Slate);
        AddAction(actions, "\uE8FB", "Verify Identity", (_, _) => VerifyIdentity(), UiTheme.Teal);
        AddAction(actions, "\uE7BA", "Manual Review", (_, _) => ManualReview(), UiTheme.Amber);
        AddAction(actions, "\uE711", "Reject", (_, _) => Reject(), UiTheme.Coral);
        AddAction(actions, "\uE72B", "Back", (_, _) => _navigation.Back(), UiTheme.Coral);

        var statusSurface = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 12, 20, 16),
            Margin = new Padding(20, 6, 20, 16),
            BackColor = UiTheme.Surface,
        };
        _providerLabel.Dock = DockStyle.Top;
        _providerLabel.Height = 40;
        _providerLabel.ForeColor = UiTheme.Muted;
        _providerLabel.Font = UiTheme.CreateFont(9f);
        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.ForeColor = UiTheme.Ink;
        _statusLabel.Font = UiTheme.CreateFont(10f, FontStyle.Bold);
        statusSurface.Controls.Add(_statusLabel);
        statusSurface.Controls.Add(_providerLabel);

        var statusHost = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Canvas, Padding = new Padding(20, 4, 20, 16) };
        statusHost.Controls.Add(statusSurface);

        Controls.Add(statusHost);
        Controls.Add(actions);
        Controls.Add(previews);
        Controls.Add(header);

        var cameraStatus = _disposableCamera!.IsLiveCameraAvailable
            ? $"Live camera: {_disposableCamera.DriverName}. Capture uses the connected webcam."
            : "Live camera: none detected. Capture will fall back to selecting an image file.";
        var faceStatus = _faceService.IsAvailable
            ? "Facial verification provider: configured."
            : "Facial verification provider: NOT configured. Verification uses manual review. A face match alone does not prove eligibility.";
        _providerLabel.Text = cameraStatus + Environment.NewLine + faceStatus;
        _statusLabel.Text = $"Current status: {_voter.VerificationStatus}";
    }

    private static RoundedPanel BuildPreviewCard(string caption, PictureBox picture)
    {
        var card = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 12, 0),
            Padding = new Padding(12),
            BackColor = UiTheme.Surface,
        };
        var label = new Label
        {
            Text = caption,
            Dock = DockStyle.Top,
            Height = 24,
            Font = UiTheme.CreateFont(9f, FontStyle.Bold),
            ForeColor = UiTheme.Muted,
        };
        picture.Dock = DockStyle.Fill;
        picture.SizeMode = PictureBoxSizeMode.Zoom;
        picture.BackColor = UiTheme.Canvas;
        card.Controls.Add(picture);
        card.Controls.Add(label);
        return card;
    }

    private static void AddAction(Control container, string glyph, string text, EventHandler handler, Color color)
    {
        var button = new IconButton(glyph, text)
        {
            Width = 175,
            Height = 42,
            BackColor = color,
            HoverColor = ControlPaint.Light(color, 0.12f),
            Margin = new Padding(0, 0, 8, 8),
        };
        button.Click += handler;
        container.Controls.Add(button);
    }

    private void LoadExistingReference()
    {
        _referenceBytes = _photoStore.TryRead(_voter.PhotoPath);
        if (_referenceBytes != null)
        {
            _referencePreview.Image = BytesToImage(_referenceBytes);
        }
    }

    private void CaptureReference()
    {
        var bytes = _camera.Capture();
        if (bytes == null)
        {
            MessageBox.Show(this, "No image was selected.", "Capture Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!PassesQualityCheck(bytes, out var qualityMessage))
        {
            MessageBox.Show(this, qualityMessage, "Image Quality", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _referenceBytes = bytes;
        _referencePreview.Image = BytesToImage(bytes);
        var path = _photoStore.SaveReference(_voter.Id, bytes);
        _voterService.SetReferencePhoto(_voter.Id, path);
        _voter.PhotoPath = path;
        _statusLabel.Text = "Reference photograph saved.";
    }

    private void ScanFace()
    {
        var bytes = _camera.Capture();
        if (bytes == null)
        {
            MessageBox.Show(this, "No image was selected.", "Scan Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!PassesQualityCheck(bytes, out var qualityMessage))
        {
            MessageBox.Show(this, qualityMessage, "Image Quality", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _capturedBytes = bytes;
        _capturePreview.Image = BytesToImage(bytes);
        _statusLabel.Text = "Scan captured. Run 'Verify Identity' to compare.";
    }

    private void Retake()
    {
        _capturedBytes = null;
        _capturePreview.Image = null;
        _statusLabel.Text = "Capture cleared. Retake the scan.";
    }

    private void VerifyIdentity()
    {
        if (_referenceBytes == null)
        {
            MessageBox.Show(this, "Capture a reference photograph first.", "Missing Reference", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_capturedBytes == null)
        {
            MessageBox.Show(this, "Scan a face capture first.", "Missing Scan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var result = _faceService.Verify(_referenceBytes, _capturedBytes);
        var status = result.Outcome switch
        {
            FaceVerificationOutcome.Verified => VoterVerificationStatus.Verified,
            FaceVerificationOutcome.ManualReview => VoterVerificationStatus.ManualReview,
            FaceVerificationOutcome.Failed => VoterVerificationStatus.Rejected,
            _ => VoterVerificationStatus.ManualReview,
        };

        _statusLabel.Text = $"Result: {result.Outcome} - {result.Message}";
        MessageBox.Show(
            this,
            result.Message,
            result.Outcome.ToString(),
            MessageBoxButtons.OK,
            result.Outcome == FaceVerificationOutcome.Verified ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

        _voterService.SetVerification(_voter.Id, status);
    }

    private void ManualReview()
    {
        var confirm = MessageBox.Show(
            this,
            $"Mark '{_voter.Name}' as Verified by manual review?\n\nYou must be satisfied that the person's identity and eligibility have been checked through an alternative, authorized process.",
            "Manual Verification",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        var result = _voterService.SetVerification(_voter.Id, VoterVerificationStatus.Verified);
        _statusLabel.Text = result.Message;
        MessageBox.Show(this, result.Message, "Verified", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void Reject()
    {
        var result = _voterService.SetVerification(_voter.Id, VoterVerificationStatus.Rejected);
        _statusLabel.Text = result.Message;
        MessageBox.Show(this, result.Message, "Rejected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private static bool PassesQualityCheck(byte[] bytes, out string message)
    {
        message = string.Empty;
        if (bytes.Length < 4 * 1024)
        {
            message = "The image is too small to be usable for verification. Please capture a larger image.";
            return false;
        }

        try
        {
            using var stream = new MemoryStream(bytes);
            using var image = Image.FromStream(stream);
            if (image.Width < 160 || image.Height < 160)
            {
                message = "The image resolution is too low. Please capture at least 160x160 pixels.";
                return false;
            }
        }
        catch (Exception)
        {
            message = "The selected file is not a readable image.";
            return false;
        }

        return true;
    }

    private static Image? BytesToImage(byte[] bytes)
    {
        try
        {
            using var stream = new MemoryStream(bytes);
            return Image.FromStream(stream);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
