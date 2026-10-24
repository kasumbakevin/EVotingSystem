using EVotingSystem.Data;
using EVotingSystem.Models;

namespace EVotingSystem.Forms;

/// <summary>
/// Public election status page: shows each election's state and schedule without
/// exposing candidate totals. Intended for voters and the public menu.
/// </summary>
public class ElectionStatusForm : Form, INavigationAware
{
    private readonly DatabaseFactory _databaseFactory;
    private readonly FormNavigationService _navigation;
    private readonly FlowLayoutPanel _list = new();
    private readonly Label _emptyLabel = new();

    public ElectionStatusForm(DatabaseFactory databaseFactory, FormNavigationService navigation)
    {
        _databaseFactory = databaseFactory;
        _navigation = navigation;

        Text = "Election Status";
        Size = new Size(860, 640);
        MinimumSize = new Size(720, 520);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        BackColor = UiTheme.Canvas;

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
            Text = "ELECTION STATUS",
            Dock = DockStyle.Top,
            Height = 36,
            Font = UiTheme.CreateFont(18f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        var subtitle = new Label
        {
            Text = "Current state and schedule of configured elections",
            Dock = DockStyle.Fill,
            Font = UiTheme.CreateFont(9.5f),
            ForeColor = UiTheme.HeaderSubtext,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        header.Controls.Add(subtitle);
        header.Controls.Add(title);

        _list.Dock = DockStyle.Fill;
        _list.FlowDirection = FlowDirection.TopDown;
        _list.WrapContents = false;
        _list.AutoScroll = true;
        _list.Padding = new Padding(20, 16, 24, 16);
        _list.BackColor = UiTheme.Canvas;
        _list.SizeChanged += (_, _) => ResizeCards();

        _emptyLabel.Text = "No elections have been configured yet.";
        _emptyLabel.Dock = DockStyle.Fill;
        _emptyLabel.TextAlign = ContentAlignment.MiddleCenter;
        _emptyLabel.ForeColor = UiTheme.Muted;
        _emptyLabel.Font = UiTheme.CreateFont(11f);

        var actions = new Panel { Dock = DockStyle.Bottom, Height = 62, BackColor = UiTheme.Canvas, Padding = new Padding(20, 10, 20, 10) };
        var back = new IconButton("\uE72B", "Back")
        {
            Dock = DockStyle.Right,
            Width = 160,
            BackColor = UiTheme.Coral,
            HoverColor = ControlPaint.Light(UiTheme.Coral, 0.12f),
        };
        back.Click += (_, _) => _navigation.Back();
        var refresh = new IconButton("\uE72C", "Refresh")
        {
            Dock = DockStyle.Left,
            Width = 160,
            BackColor = UiTheme.Ocean,
            HoverColor = ControlPaint.Light(UiTheme.Ocean, 0.12f),
        };
        refresh.Click += (_, _) => LoadStatus();
        actions.Controls.Add(back);
        actions.Controls.Add(refresh);

        Controls.Add(_list);
        Controls.Add(_emptyLabel);
        Controls.Add(actions);
        Controls.Add(header);

        LoadStatus();
    }

    public void RequestBack() => _navigation.Back();

    private void ResizeCards()
    {
        int width = Math.Max(400, _list.ClientSize.Width - _list.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth);
        foreach (Control control in _list.Controls)
        {
            control.Width = width;
        }
    }

    private void LoadStatus()
    {
        _list.Controls.Clear();
        using var context = _databaseFactory.CreateContext();
        var elections = context.Elections.OrderBy(e => e.Title).ToList();

        _emptyLabel.Visible = elections.Count == 0;
        _list.Visible = elections.Count > 0;

        foreach (var election in elections)
        {
            _list.Controls.Add(BuildCard(election, context));
        }

        ResizeCards();
    }

    private static RoundedPanel BuildCard(Election election, AppDbContext context)
    {
        var card = new RoundedPanel
        {
            Height = 132,
            Margin = new Padding(0, 0, 0, 14),
            BackColor = UiTheme.Surface,
            Padding = new Padding(20, 14, 20, 14),
        };

        var title = new Label
        {
            Text = election.Title,
            Font = UiTheme.CreateFont(13f, FontStyle.Bold),
            ForeColor = UiTheme.Ink,
            Location = new Point(20, 14),
            AutoSize = true,
        };

        var position = new Label
        {
            Text = $"Position: {election.Position}",
            Font = UiTheme.CreateFont(9.5f),
            ForeColor = UiTheme.Muted,
            Location = new Point(20, 42),
            AutoSize = true,
        };

        string schedule = $"Opens: {Format(election.OpensAt)}    Closes: {Format(election.ClosesAt)}";
        var scheduleLabel = new Label
        {
            Text = schedule,
            Font = UiTheme.CreateFont(9f),
            ForeColor = UiTheme.Muted,
            Location = new Point(20, 66),
            AutoSize = true,
        };

        int candidateCount = context.Candidates.Count(c => c.ElectionId == election.Id);
        var candidatesLabel = new Label
        {
            Text = $"{candidateCount} candidate(s) registered",
            Font = UiTheme.CreateFont(9f, FontStyle.Bold),
            ForeColor = UiTheme.Teal,
            Location = new Point(20, 90),
            AutoSize = true,
        };

        var badge = UiTheme.CreateStatusBadge(election.Status.ToString(), UiTheme.StatusColor(election.Status));
        badge.Location = new Point(card.Width, 0);
        badge.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        badge.Left = 0;
        badge.Top = 16;
        card.Controls.Add(badge);
        card.Layout += (_, _) => badge.Left = Math.Max(20, card.ClientSize.Width - badge.Width - 20);

        card.Controls.Add(title);
        card.Controls.Add(position);
        card.Controls.Add(scheduleLabel);
        card.Controls.Add(candidatesLabel);
        return card;
    }

    private static string Format(DateTime? value)
        => value?.ToLocalTime().ToString("dd MMM yyyy HH:mm") ?? "-";
}
