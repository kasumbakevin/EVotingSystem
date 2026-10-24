using EVotingSystem.Data;
using EVotingSystem.Models;
using EVotingSystem.Services;

namespace EVotingSystem.Forms;

/// <summary>
/// Two-column voter registration and identity-verification screen.
///
/// Left: voter information captured and validated before save.
/// Right: webcam capture + optional facial verification. Where no facial
/// verification provider is configured, an authorized manual-verification flow is
/// offered instead — a face match is never treated as sufficient proof of
/// eligibility, and biometric images are stored apart from ballot data.
/// </summary>
public class VoterRegistrationForm : Form, INavigationAware
{
    private readonly VoterService _voterService;
    private readonly AuditService _auditService;
    private readonly PhotoStore _photoStore;
    private readonly IFaceVerificationService _faceService = new UnavailableFaceVerificationService();
    private readonly CameraCapture _camera = new();

    private readonly FormNavigationService _navigation;

    private readonly TextBox _voterNumber = new();
    private readonly TextBox _fullName = new();
    private readonly TextBox _department = new();
    private readonly TextBox _course = new();
    private readonly TextBox _email = new();
    private readonly TextBox _phone = new();
    private readonly ComboBox _eligibility = new();
    private readonly TextBox _username = new();
    private readonly TextBox _password = new();
    private readonly Label _validationLabel = new();

    private readonly PictureBox _preview = new();
    private readonly RoundedPanel _verificationBadge;
    private readonly Label _verificationLabel = new();
    private readonly Label _cameraLabel = new();

    private byte[]? _referenceBytes;
    private byte[]? _capturedBytes;

    public VoterRegistrationForm(DatabaseFactory databaseFactory, FormNavigationService navigation)
    {
        _voterService = new VoterService(databaseFactory);
        _auditService = new AuditService(databaseFactory);
        _photoStore = new PhotoStore();
        _navigation = navigation;

        Text = "Voter Registration";
        Size = new Size(1180, 760);
        MinimumSize = new Size(1040, 660);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        BackColor = UiTheme.Canvas;

        _verificationBadge = UiTheme.CreateStatusBadge("Not Verified", UiTheme.Muted);
        BuildInterface();
        SetVerificationState("Not Verified", UiTheme.Muted);
        FormClosed += (_, _) => _camera.Dispose();
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
            Text = "VOTER REGISTRATION",
            Dock = DockStyle.Top,
            Height = 36,
            Font = UiTheme.CreateFont(18f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        var subtitle = new Label
        {
            Text = "Capture voter details and verify identity with the webcam",
            Dock = DockStyle.Fill,
            Font = UiTheme.CreateFont(9.5f),
            ForeColor = UiTheme.HeaderSubtext,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        header.Controls.Add(subtitle);
        header.Controls.Add(title);

        var columns = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(20, 16, 20, 8),
            BackColor = UiTheme.Canvas,
        };
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        columns.Controls.Add(BuildInformationCard(), 0, 0);
        columns.Controls.Add(BuildCaptureCard(), 1, 0);

        _validationLabel.Dock = DockStyle.Bottom;
        _validationLabel.Height = 34;
        _validationLabel.Padding = new Padding(24, 0, 24, 0);
        _validationLabel.TextAlign = ContentAlignment.MiddleLeft;
        _validationLabel.Font = UiTheme.CreateFont(9f, FontStyle.Bold);
        _validationLabel.ForeColor = UiTheme.Coral;

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = UiTheme.Canvas,
            Padding = new Padding(20, 8, 20, 8),
        };
        AddAction(actions, "\uE72B", "Back", (_, _) => _navigation.Back(), UiTheme.Coral);
        AddAction(actions, "\uE711", "Clear", (_, _) => ClearForm(), UiTheme.Slate);
        AddAction(actions, "\uE74E", "Save Voter", (_, _) => SaveVoter(closeAfterSave: false), UiTheme.Green);
        AddAction(actions, "\uE72A", "Save & Next", (_, _) => SaveVoter(closeAfterSave: true), UiTheme.Teal);

        Controls.Add(columns);
        Controls.Add(actions);
        Controls.Add(_validationLabel);
        Controls.Add(header);
    }

    private RoundedPanel BuildInformationCard()
    {
        var card = new SectionCard("Voter information", 44)
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 10, 0),
        };

        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            AutoScroll = true,
            BackColor = UiTheme.Surface,
        };

        AddField(fields, "Voter number *", _voterNumber);
        AddField(fields, "Full name *", _fullName);
        AddField(fields, "Department / Constituency", _department);
        AddField(fields, "Course / Category", _course);
        AddField(fields, "Email", _email);
        AddField(fields, "Phone number", _phone);

        var eligibilityLabel = new Label { Text = "Eligibility status", Dock = DockStyle.Top, Height = 20, Font = UiTheme.CreateFont(8.5f, FontStyle.Bold), ForeColor = UiTheme.Ink };
        _eligibility.Dock = DockStyle.Top;
        _eligibility.DropDownStyle = ComboBoxStyle.DropDownList;
        _eligibility.Font = UiTheme.CreateFont(10f);
        _eligibility.Items.AddRange(new object[] { "Pending Verification", "Eligible", "Ineligible" });
        _eligibility.SelectedIndex = 0;
        var eligibilityHost = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = UiTheme.Surface };
        eligibilityHost.Controls.Add(_eligibility);
        eligibilityHost.Controls.Add(eligibilityLabel);
        fields.Controls.Add(eligibilityHost);

        AddField(fields, "Username *", _username);
        AddField(fields, "Password *", _password, isPassword: true);

        card.Body.Controls.Add(fields);
        return card;
    }

    private static void AddField(TableLayoutPanel panel, string label, TextBox textBox, bool isPassword = false)
    {
        var host = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = UiTheme.Surface };
        var caption = new Label { Text = label, Dock = DockStyle.Top, Height = 20, Font = UiTheme.CreateFont(8.5f, FontStyle.Bold), ForeColor = UiTheme.Ink };
        textBox.Dock = DockStyle.Top;
        textBox.Font = UiTheme.CreateFont(10f);
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.UseSystemPasswordChar = isPassword;
        host.Controls.Add(textBox);
        host.Controls.Add(caption);
        panel.Controls.Add(host);
    }

    private RoundedPanel BuildCaptureCard()
    {
        var card = new SectionCard("Webcam capture & face verification", 44)
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(10, 0, 0, 0),
        };

        _cameraLabel.Dock = DockStyle.Top;
        _cameraLabel.Height = 22;
        _cameraLabel.Font = UiTheme.CreateFont(8.5f);
        _cameraLabel.ForeColor = UiTheme.Muted;
        _cameraLabel.Text = _camera.IsLiveCameraAvailable
            ? $"Live camera: {_camera.DriverName}"
            : "Live camera not detected. Capture will fall back to selecting an image file.";

        _preview.Dock = DockStyle.Fill;
        _preview.SizeMode = PictureBoxSizeMode.Zoom;
        _preview.BackColor = UiTheme.Canvas;

        var badgeRow = new Panel { Dock = DockStyle.Bottom, Height = 34, BackColor = UiTheme.Surface };
        _verificationBadge.Dock = DockStyle.Left;
        _verificationBadge.Width = 130;
        _verificationLabel.Dock = DockStyle.Fill;
        _verificationLabel.TextAlign = ContentAlignment.MiddleLeft;
        _verificationLabel.Font = UiTheme.CreateFont(9f);
        _verificationLabel.Padding = new Padding(10, 0, 0, 0);
        _verificationLabel.ForeColor = UiTheme.Muted;
        _verificationLabel.Text = _faceService.IsAvailable
            ? "Facial verification configured."
            : "No facial-verification provider configured. Use manual review.";
        badgeRow.Controls.Add(_verificationLabel);
        badgeRow.Controls.Add(_verificationBadge);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 100, BackColor = UiTheme.Surface, WrapContents = true };
        AddSmallButton(buttons, "\uE722", "Start / Capture", (_, _) => CaptureReference(), UiTheme.Green);
        AddSmallButton(buttons, "\uE72C", "Retake", (_, _) => Retake(), UiTheme.Slate);
        AddSmallButton(buttons, "\uE8B7", "Scan Face", (_, _) => ScanFace(), UiTheme.Ocean);
        AddSmallButton(buttons, "\uE8FB", "Verify", (_, _) => Verify(), UiTheme.Teal);
        AddSmallButton(buttons, "\uE7BA", "Manual Review", (_, _) => ManualReview(), UiTheme.Amber);

        var previewHost = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface, Padding = new Padding(0, 6, 0, 6) };
        previewHost.Controls.Add(_preview);

        card.Body.Controls.Add(previewHost);
        card.Body.Controls.Add(buttons);
        card.Body.Controls.Add(badgeRow);
        card.Body.Controls.Add(_cameraLabel);
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
            Margin = new Padding(0, 0, 10, 8),
        };
        button.Click += handler;
        container.Controls.Add(button);
    }

    private static void AddSmallButton(Control container, string glyph, string text, EventHandler handler, Color color)
    {
        var button = new IconButton(glyph, text)
        {
            Width = 200,
            Height = 40,
            BackColor = color,
            HoverColor = ControlPaint.Light(color, 0.12f),
            Margin = new Padding(0, 0, 8, 8),
        };
        button.Click += handler;
        container.Controls.Add(button);
    }

    private void SetVerificationState(string text, Color color)
    {
        UiTheme.SetStatusBadge(_verificationBadge, text, color);
    }

    private void CaptureReference()
    {
        var bytes = _camera.Capture();
        if (bytes == null)
        {
            return;
        }

        _referenceBytes = bytes;
        _preview.Image = BytesToImage(bytes);
        RetakeCaptureOnly();
        SetVerificationState("Ready to Scan", UiTheme.Ocean);
        _verificationLabel.Text = "Reference captured. Capture a live scan to verify identity.";
    }

    private void ScanFace()
    {
        var bytes = _camera.Capture();
        if (bytes == null)
        {
            return;
        }

        _capturedBytes = bytes;
        _preview.Image = BytesToImage(bytes);
        SetVerificationState("Ready to Scan", UiTheme.Ocean);
        _verificationLabel.Text = "Scan captured. Press Verify to compare with the reference.";
    }

    private void Retake()
    {
        RetakeCaptureOnly();
        _referenceBytes = null;
        _capturedBytes = null;
        _preview.Image = null;
        SetVerificationState("Not Verified", UiTheme.Muted);
        _verificationLabel.Text = "Capture cleared. Start again.";
    }

    private void RetakeCaptureOnly()
    {
        _capturedBytes = null;
    }

    private void Verify()
    {
        if (_referenceBytes == null || _capturedBytes == null)
        {
            MessageBox.Show(this, "Capture a reference photo and a scan before verifying.", "Missing Capture", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var result = _faceService.Verify(_referenceBytes, _capturedBytes);
        switch (result.Outcome)
        {
            case FaceVerificationOutcome.Verified:
                SetVerificationState("Verified", UiTheme.Success);
                break;
            case FaceVerificationOutcome.RetryRequired:
                SetVerificationState("Retry Required", UiTheme.Amber);
                break;
            case FaceVerificationOutcome.ManualReview:
                SetVerificationState("Manual Review", UiTheme.Amber);
                break;
            case FaceVerificationOutcome.Failed:
                SetVerificationState("Not Verified", UiTheme.Coral);
                break;
            default:
                SetVerificationState("Not Verified", UiTheme.Muted);
                break;
        }

        _verificationLabel.Text = result.Message;
        MessageBox.Show(this, result.Message, "Face Verification", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ManualReview()
    {
        var confirm = MessageBox.Show(
            this,
            "Record that this voter's identity was checked through an authorized alternative process?\n\nFacial verification is unavailable, so an officer must confirm eligibility manually.",
            "Manual Verification",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        SetVerificationState("Manual Review", UiTheme.Amber);
        _verificationLabel.Text = "Marked for manual review. Approve in Voter Management after checking documents.";
    }

    private void ClearForm()
    {
        foreach (var box in new[] { _voterNumber, _fullName, _department, _course, _email, _phone, _username, _password })
        {
            box.Clear();
        }

        _eligibility.SelectedIndex = 0;
        Retake();
        _validationLabel.Text = string.Empty;
    }

    private void SaveVoter(bool closeAfterSave)
    {
        _validationLabel.Text = string.Empty;

        if (string.IsNullOrWhiteSpace(_fullName.Text) || string.IsNullOrWhiteSpace(_voterNumber.Text)
            || string.IsNullOrWhiteSpace(_username.Text) || string.IsNullOrWhiteSpace(_password.Text))
        {
            _validationLabel.Text = "Voter number, full name, username and password are required.";
            return;
        }

        if (_password.Text.Length < 8)
        {
            _validationLabel.Text = "Password must be at least 8 characters.";
            return;
        }

        var result = _voterService.Register(
            _fullName.Text.Trim(),
            _username.Text.Trim(),
            _password.Text,
            _voterNumber.Text.Trim(),
            NullIfEmpty(_department.Text),
            NullIfEmpty(_email.Text),
            NullIfEmpty(_phone.Text));

        if (!result.Success)
        {
            _validationLabel.Text = result.Message;
            return;
        }

        var voter = _voterService.Search(_voterNumber.Text.Trim()).FirstOrDefault(v => v.VoterNumber == _voterNumber.Text.Trim());
        if (voter != null && _referenceBytes != null)
        {
            var path = _photoStore.SaveReference(voter.Id, _referenceBytes);
            _voterService.SetReferencePhoto(voter.Id, path);
        }

        _auditService.Record(AuditCategories.Voter, "Voter registered", $"Registered '{_fullName.Text.Trim()}' ({_voterNumber.Text.Trim()}).", "Admin");

        MessageBox.Show(this, result.Message, "Voter Registered", MessageBoxButtons.OK, MessageBoxIcon.Information);

        if (closeAfterSave)
        {
            ClearForm();
        }
        else
        {
            _navigation.Back();
        }
    }

    private static string? NullIfEmpty(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
