using EVotingSystem.Data;
using EVotingSystem.Services;

namespace EVotingSystem.Forms;

/// <summary>
/// Public settings screen. Exposes email/SMTP configuration for invitations and
/// general system information. Administrator-only actions remain guarded by the
/// admin dashboard.
/// </summary>
public class SettingsForm : Form, INavigationAware
{
    private readonly DatabaseFactory _databaseFactory;
    private readonly FormNavigationService _navigation;
    private readonly SettingsService _settingsService;
    private readonly Label _emailStateLabel = new();

    public SettingsForm(DatabaseFactory databaseFactory, FormNavigationService navigation)
    {
        _databaseFactory = databaseFactory;
        _navigation = navigation;
        _settingsService = new SettingsService(databaseFactory);

        Text = "Settings";
        Size = new Size(840, 620);
        MinimumSize = new Size(720, 520);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        BackColor = UiTheme.Canvas;

        BuildInterface();
        RefreshState();
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
            Text = "SETTINGS",
            Dock = DockStyle.Top,
            Height = 36,
            Font = UiTheme.CreateFont(18f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        var subtitle = new Label
        {
            Text = "Application, database and email configuration",
            Dock = DockStyle.Fill,
            Font = UiTheme.CreateFont(9.5f),
            ForeColor = UiTheme.HeaderSubtext,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        header.Controls.Add(subtitle);
        header.Controls.Add(title);

        var body = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Canvas, Padding = new Padding(20, 16, 20, 16), AutoScroll = true };

        var emailCard = new SectionCard("Email / SMTP configuration", 44)
        {
            Height = 150,
            Dock = DockStyle.Top,
        };
        _emailStateLabel.Dock = DockStyle.Top;
        _emailStateLabel.Height = 70;
        _emailStateLabel.Font = UiTheme.CreateFont(9.5f);
        _emailStateLabel.ForeColor = UiTheme.Ink;
        emailCard.Body.Controls.Add(_emailStateLabel);

        var configureButton = new IconButton("\uE713", "Configure Email")
        {
            Dock = DockStyle.Bottom,
            Width = 220,
            BackColor = UiTheme.Green,
            HoverColor = ControlPaint.Light(UiTheme.Green, 0.12f),
        };
        configureButton.Click += (_, _) =>
        {
            using var dialog = new EmailSettingsDialog(_settingsService);
            dialog.ShowDialog(this);
            RefreshState();
        };
        emailCard.Body.Controls.Add(configureButton);

        var infoCard = new SectionCard("System information", 44)
        {
            Height = 190,
            Dock = DockStyle.Top,
            Padding = new Padding(16, 12, 16, 12),
        };
        var infoLabel = new Label
        {
            Dock = DockStyle.Fill,
            Font = UiTheme.CreateFont(9.5f),
            ForeColor = UiTheme.Muted,
            Text = BuildSystemInfo(),
        };
        infoCard.Body.Controls.Add(infoLabel);

        body.Controls.Add(infoCard);
        body.Controls.Add(emailCard);

        var actions = new Panel { Dock = DockStyle.Bottom, Height = 62, BackColor = UiTheme.Canvas, Padding = new Padding(20, 10, 20, 10) };
        var back = new IconButton("\uE72B", "Back")
        {
            Dock = DockStyle.Right,
            Width = 160,
            BackColor = UiTheme.Coral,
            HoverColor = ControlPaint.Light(UiTheme.Coral, 0.12f),
        };
        back.Click += (_, _) => _navigation.Back();
        actions.Controls.Add(back);

        Controls.Add(body);
        Controls.Add(actions);
        Controls.Add(header);
    }

    private string BuildSystemInfo()
    {
        using var context = _databaseFactory.CreateContext();
        return string.Join(Environment.NewLine, new[]
        {
            $"Database: {_databaseFactory.DatabasePath}",
            $"Elections: {context.Elections.Count()}",
            $"Candidates: {context.Candidates.Count()}",
            $"Voters: {context.Voters.Count()}",
            $"Audit records: {context.AuditLogs.Count()}",
            string.Empty,
            "Passwords are stored as salted hashes (PBKDF2). Ballots are stored separately",
            "from participation receipts so a vote cannot be linked back to a voter.",
        });
    }

    private void RefreshState()
    {
        var settings = _settingsService.GetEmailSettings();
        _emailStateLabel.Text = settings.IsConfigured
            ? $"SMTP enabled.\nHost: {settings.SmtpHost}:{settings.SmtpPort}\nFrom: {settings.FromAddress}\nPublic base URL: {settings.PublicBaseUrl}"
            : "Email delivery is NOT configured. Invitations will be exported as CSV for manual distribution.";
        _emailStateLabel.ForeColor = settings.IsConfigured ? UiTheme.Success : UiTheme.Amber;
    }
}
