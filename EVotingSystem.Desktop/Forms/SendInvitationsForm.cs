using EVotingSystem.Data;
using EVotingSystem.Models;
using EVotingSystem.Services;

namespace EVotingSystem.Forms;

/// <summary>
/// Lets an administrator generate and dispatch one-time voting invitations.
/// When no email/SMS provider is configured links are exported for manual
/// distribution and the UI states clearly that nothing was sent.
/// </summary>
public class SendInvitationsForm : Form, INavigationAware
{
    private readonly VoterService _voterService;
    private readonly SettingsService _settingsService;
    private readonly DatabaseFactory _databaseFactory;
    private readonly FormNavigationService _navigation;
    private InvitationService _invitationService;

    private readonly ListView _list = new();
    private readonly Label _statusLabel = new();
    private readonly string _exportDirectory;

    public SendInvitationsForm(DatabaseFactory databaseFactory, FormNavigationService navigation)
    {
        _databaseFactory = databaseFactory;
        _voterService = new VoterService(databaseFactory);
        _settingsService = new SettingsService(databaseFactory);
        _exportDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EVotingSystem",
            "invitations");
        _invitationService = BuildInvitationService(databaseFactory);
        _navigation = navigation;

        Text = "Send Voting Invitations";
        Size = new Size(900, 640);
        MinimumSize = new Size(820, 560);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        BackColor = UiTheme.Canvas;

        BuildInterface();
        RefreshList();
        Activated += (_, _) =>
        {
            _invitationService = BuildInvitationService(databaseFactory);
            UpdateStatusHint();
        };
    }

    private InvitationService BuildInvitationService(DatabaseFactory databaseFactory)
    {
        var settings = _settingsService.GetEmailSettings();
        IInvitationSender sender = settings.IsConfigured
            ? new SmtpInvitationSender(settings)
            : new CsvExportInvitationSender(_exportDirectory);

        return new InvitationService(databaseFactory, sender, settings.PublicBaseUrl);
    }

    private void UpdateStatusHint()
    {
        _statusLabel.Text = _invitationService.IsProviderConfigured
            ? "SMTP is configured. Invitations will be emailed directly."
            : "No SMTP provider is configured. Invitations will be EXPORTED for manual distribution and NOT sent. Use Email Settings to configure SMTP.";
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
            Text = "VOTING INVITATIONS",
            Dock = DockStyle.Top,
            Height = 34,
            Font = UiTheme.CreateFont(18f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
        };
        var subtitle = new Label
        {
            Text = "Generate one-time links so verified voters can vote online",
            Dock = DockStyle.Fill,
            Font = UiTheme.CreateFont(9.5f),
            ForeColor = UiTheme.HeaderSubtext,
            BackColor = Color.Transparent,
        };
        header.Controls.Add(subtitle);
        header.Controls.Add(title);

        _list.Dock = DockStyle.Fill;
        _list.View = View.Details;
        _list.CheckBoxes = true;
        _list.FullRowSelect = true;
        _list.GridLines = false;
        _list.HideSelection = false;
        _list.Font = UiTheme.CreateFont(10f);
        _list.Columns.Add("Voter Number", 140);
        _list.Columns.Add("Name", 200);
        _list.Columns.Add("Email", 220);
        _list.Columns.Add("Status", 130);
        _list.Columns.Add("Last Invitation", 150);

        var listHost = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Canvas, Padding = new Padding(20, 12, 20, 8) };
        listHost.Controls.Add(_list);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = UiTheme.Canvas,
            Padding = new Padding(20, 8, 20, 4),
        };
        AddAction(actions, "\uE8F4", "Select Verified", (_, _) => SelectVerified(), UiTheme.Ocean);
        AddAction(actions, "\uE8BD", "Send / Export Invitations", (_, _) => SendInvitations(), UiTheme.Green);
        AddAction(actions, "\uE715", "Email Settings", (_, _) => OpenEmailSettings(), UiTheme.Slate);
        AddAction(actions, "\uE72B", "Back", (_, _) => _navigation.Back(), UiTheme.Coral);

        _statusLabel.Dock = DockStyle.Bottom;
        _statusLabel.Height = 44;
        _statusLabel.Padding = new Padding(22, 0, 22, 0);
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusLabel.Font = UiTheme.CreateFont(9.5f);
        _statusLabel.ForeColor = UiTheme.Ink;
        _statusLabel.BackColor = UiTheme.Mint;
        UpdateStatusHint();

        Controls.Add(listHost);
        Controls.Add(actions);
        Controls.Add(_statusLabel);
        Controls.Add(header);
    }

    private static void AddAction(Control container, string glyph, string text, EventHandler handler, Color color)
    {
        var button = new IconButton(glyph, text)
        {
            Width = 230,
            Height = 42,
            BackColor = color,
            HoverColor = ControlPaint.Light(color, 0.12f),
            Margin = new Padding(0, 0, 8, 8),
        };
        button.Click += handler;
        container.Controls.Add(button);
    }

    private void RefreshList()
    {
        _list.Items.Clear();
        foreach (var voter in _voterService.GetAll().OrderBy(v => v.Name))
        {
            var item = new ListViewItem(voter.VoterNumber) { Tag = voter.Id };
            item.SubItems.Add(voter.Name);
            item.SubItems.Add(voter.Email ?? "(none)");
            item.SubItems.Add(voter.VerificationStatus.ToString());
            item.SubItems.Add(voter.InvitationSentAt?.ToLocalTime().ToString("g") ?? "Never");
            item.ForeColor = voter.VerificationStatus == VoterVerificationStatus.Verified ? UiTheme.Ink : UiTheme.Muted;
            _list.Items.Add(item);
        }
    }

    private void SelectVerified()
    {
        foreach (ListViewItem item in _list.Items)
        {
            item.Checked = item.SubItems[3].Text == VoterVerificationStatus.Verified.ToString();
        }
    }

    private void SendInvitations()
    {
        var ids = _list.CheckedItems
            .Cast<ListViewItem>()
            .Select(i => (int)i.Tag!)
            .ToList();

        if (ids.Count == 0)
        {
            MessageBox.Show(this, "Select at least one voter.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var result = _invitationService.SendInvitations(ids);

        var message = result.ProviderConfigured
            ? $"{result.Message}"
            : $"No SMTP provider configured. {result.Failed} invitation link(s) were exported to:\n{result.ExportPath}\n\nThey have NOT been sent.";

        _statusLabel.Text = result.Message;
        MessageBox.Show(this, message, "Invitations", MessageBoxButtons.OK, result.ProviderConfigured && result.Failed == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        RefreshList();
    }

    private void OpenEmailSettings()
    {
        using var dialog = new EmailSettingsDialog(_settingsService);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _invitationService = BuildInvitationService(_databaseFactory);
            UpdateStatusHint();
        }
    }
}
