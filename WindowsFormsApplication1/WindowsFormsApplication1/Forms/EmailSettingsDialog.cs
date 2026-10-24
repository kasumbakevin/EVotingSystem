using EVotingSystem.Models;
using EVotingSystem.Services;

namespace EVotingSystem.Forms;

/// <summary>
/// Lets an administrator configure the SMTP server used to deliver voter
/// invitations. A test email can be sent to confirm the settings before saving.
/// </summary>
public class EmailSettingsDialog : Form
{
    private readonly SettingsService _settingsService;

    private readonly CheckBox _enabled = new();
    private readonly TextBox _host = new();
    private readonly NumericUpDown _port = new();
    private readonly CheckBox _useStartTls = new();
    private readonly TextBox _username = new();
    private readonly TextBox _password = new();
    private readonly TextBox _fromAddress = new();
    private readonly TextBox _fromName = new();
    private readonly TextBox _baseUrl = new();
    private readonly TextBox _testRecipient = new();

    public EmailSettingsDialog(SettingsService settingsService)
    {
        _settingsService = settingsService;

        Text = "Email (SMTP) Settings";
        Size = new Size(560, 560);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var settings = _settingsService.GetEmailSettings();

        var enabledLabel = new Label { Text = "Enable email delivery:", Location = new Point(24, 26), AutoSize = true };
        _enabled.Location = new Point(220, 24);
        _enabled.Text = "Send invitations by email when enabled";
        _enabled.AutoSize = true;
        _enabled.Checked = settings.Enabled;

        var hostLabel = new Label { Text = "SMTP host:", Location = new Point(24, 66), AutoSize = true };
        _host.Location = new Point(220, 62);
        _host.Width = 300;
        _host.Text = settings.SmtpHost;

        var portLabel = new Label { Text = "Port:", Location = new Point(24, 106), AutoSize = true };
        _port.Location = new Point(220, 102);
        _port.Width = 100;
        _port.Minimum = 1;
        _port.Maximum = 65535;
        _port.Value = settings.SmtpPort is > 0 and <= 65535 ? settings.SmtpPort : 587;

        _useStartTls.Location = new Point(340, 104);
        _useStartTls.Text = "Use TLS/SSL";
        _useStartTls.AutoSize = true;
        _useStartTls.Checked = settings.UseStartTls;

        var userLabel = new Label { Text = "Username:", Location = new Point(24, 146), AutoSize = true };
        _username.Location = new Point(220, 142);
        _username.Width = 300;
        _username.Text = settings.Username;

        var passLabel = new Label { Text = "Password:", Location = new Point(24, 186), AutoSize = true };
        _password.Location = new Point(220, 182);
        _password.Width = 300;
        _password.UseSystemPasswordChar = true;
        _password.Text = settings.Password;

        var fromLabel = new Label { Text = "From address:", Location = new Point(24, 226), AutoSize = true };
        _fromAddress.Location = new Point(220, 222);
        _fromAddress.Width = 300;
        _fromAddress.Text = settings.FromAddress;

        var fromNameLabel = new Label { Text = "From name:", Location = new Point(24, 266), AutoSize = true };
        _fromName.Location = new Point(220, 262);
        _fromName.Width = 300;
        _fromName.Text = settings.FromName;

        var baseUrlLabel = new Label { Text = "Public base URL:", Location = new Point(24, 306), AutoSize = true };
        _baseUrl.Location = new Point(220, 302);
        _baseUrl.Width = 300;
        _baseUrl.Text = settings.PublicBaseUrl;

        var testLabel = new Label { Text = "Test recipient:", Location = new Point(24, 350), AutoSize = true };
        _testRecipient.Location = new Point(220, 346);
        _testRecipient.Width = 180;

        var testButton = new Button { Text = "Send Test Email", Location = new Point(410, 344), Width = 110, Height = 30 };
        testButton.Click += (_, _) => SendTestEmail();

        var saveButton = new Button { Text = "Save", Location = new Point(220, 400), Width = 140, Height = 36 };
        var cancelButton = new Button { Text = "Cancel", Location = new Point(380, 400), Width = 140, Height = 36 };

        saveButton.Click += (_, _) =>
        {
            _settingsService.SaveEmailSettings(Collect());
            MessageBox.Show(this, "Email settings saved.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        };

        cancelButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        Controls.AddRange(new Control[]
        {
            enabledLabel, _enabled,
            hostLabel, _host,
            portLabel, _port, _useStartTls,
            userLabel, _username,
            passLabel, _password,
            fromLabel, _fromAddress,
            fromNameLabel, _fromName,
            baseUrlLabel, _baseUrl,
            testLabel, _testRecipient, testButton,
            saveButton, cancelButton,
        });

        UiTheme.ApplyDialogStyle(this);
    }

    private EmailSettings Collect() => new()
    {
        Enabled = _enabled.Checked,
        SmtpHost = _host.Text.Trim(),
        SmtpPort = (int)_port.Value,
        UseStartTls = _useStartTls.Checked,
        Username = _username.Text.Trim(),
        Password = _password.Text,
        FromAddress = _fromAddress.Text.Trim(),
        FromName = _fromName.Text.Trim(),
        PublicBaseUrl = _baseUrl.Text.Trim(),
    };

    private void SendTestEmail()
    {
        var settings = Collect();
        if (!settings.IsConfigured)
        {
            MessageBox.Show(this, "Fill in the SMTP host and from address first.", "Not Configured", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(_testRecipient.Text))
        {
            MessageBox.Show(this, "Enter a test recipient address.", "Missing Recipient", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var sender = new SmtpInvitationSender(settings);
        var result = sender.Send(new[]
        {
            new InvitationRequest("Test Recipient", _testRecipient.Text.Trim(), null, settings.PublicBaseUrl),
        });

        MessageBox.Show(
            this,
            result.Sent > 0 ? "Test email sent." : result.Message,
            "Test Email",
            MessageBoxButtons.OK,
            result.Sent > 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
    }
}
