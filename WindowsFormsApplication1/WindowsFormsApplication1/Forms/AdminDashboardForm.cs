using EVotingSystem.Data;
using EVotingSystem.Models;
using EVotingSystem.Services;

namespace EVotingSystem.Forms;

public class AdminDashboardForm : Form, INavigationAware
{
    private readonly Admin _admin;
    private readonly ElectionService _electionService;
    private readonly DatabaseFactory _databaseFactory;
    private readonly FormNavigationService _navigation;
    private readonly Label _voterCountLabel;
    private readonly Label _turnoutLabel;
    private readonly Label _ballotCountLabel;
    private readonly Label _electionStateLabel;
    private readonly VoteBarChart _voteChart;
    private readonly Panel _chartViewport;
    private readonly ComboBox _electionSelector = new();
    private readonly System.Windows.Forms.Timer _autoRefreshTimer = new();

    private Election? _selectedElection;

    public AdminDashboardForm(Admin admin, ElectionService electionService, DatabaseFactory databaseFactory, FormNavigationService navigation)
    {
        _admin = admin;
        _electionService = electionService;
        _databaseFactory = databaseFactory;
        _navigation = navigation;
        Text = "Election Analytics | Admin";
        Size = new Size(1080, 780);
        MinimumSize = new Size(900, 680);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.MaximizeBox = true;
        this.MinimizeBox = true;
        BackColor = UiTheme.Canvas;

        _voteChart = new VoteBarChart { Dock = DockStyle.Top };
        _chartViewport = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = UiTheme.Surface };
        _chartViewport.Controls.Add(_voteChart);
        _chartViewport.SizeChanged += (_, _) => _voteChart.Width = Math.Max(1, _chartViewport.ClientSize.Width - SystemInformation.VerticalScrollBarWidth);

        _voterCountLabel = new Label();
        _turnoutLabel = new Label();
        _ballotCountLabel = new Label();
        _electionStateLabel = new Label();

        InitializeInterface();
        LoadElections();
        RefreshAnalytics();

        _autoRefreshTimer.Interval = 15000;
        _autoRefreshTimer.Tick += (_, _) => RefreshAnalytics();
        _autoRefreshTimer.Start();
    }

    private void InitializeInterface()
    {
        var header = new GradientPanel
        {
            Dock = DockStyle.Top,
            Height = 108,
            Padding = new Padding(24, 14, 24, 12),
            StartColor = UiTheme.HeaderStart,
            EndColor = UiTheme.HeaderEnd,
        };

        var title = new Label
        {
            Text = "ELECTION ANALYTICS",
            Font = UiTheme.CreateFont(20f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Dock = DockStyle.Top,
            Height = 40,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        var subtitle = new Label
        {
            Text = "Live participation and candidate performance",
            Font = UiTheme.CreateFont(9.5f),
            ForeColor = UiTheme.HeaderSubtext,
            BackColor = Color.Transparent,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        header.Controls.Add(subtitle);
        header.Controls.Add(title);

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(20, 14, 20, 18),
            BackColor = UiTheme.Canvas,
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));

        var selectorRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            BackColor = UiTheme.Canvas,
            Padding = new Padding(0, 6, 0, 0),
        };
        var selectorLabel = new Label { Text = "Election:", AutoSize = true, Margin = new Padding(0, 8, 8, 0), Font = UiTheme.CreateFont(9.5f, FontStyle.Bold), ForeColor = UiTheme.Ink };
        _electionSelector.Width = 380;
        _electionSelector.DropDownStyle = ComboBoxStyle.DropDownList;
        _electionSelector.SelectedIndexChanged += (_, _) =>
        {
            _selectedElection = _electionSelector.SelectedItem as Election;
            RefreshAnalytics();
        };
        selectorRow.Controls.Add(selectorLabel);
        selectorRow.Controls.Add(_electionSelector);

        var metrics = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            BackColor = UiTheme.Canvas,
            Margin = Padding.Empty,
        };
        metrics.SizeChanged += (_, _) =>
        {
            int cardWidth = Math.Max(160, (metrics.ClientSize.Width - 40) / 4);
            foreach (Control control in metrics.Controls)
            {
                control.Width = cardWidth;
            }
        };
        metrics.Controls.Add(CreateMetricCard("\uE77B", "REGISTERED VOTERS", UiTheme.Green, _voterCountLabel));
        metrics.Controls.Add(CreateMetricCard("\uE8D7", "TURNOUT", UiTheme.Success, _turnoutLabel));
        metrics.Controls.Add(CreateMetricCard("\uE8D4", "BALLOTS CAST", UiTheme.Ocean, _ballotCountLabel));
        metrics.Controls.Add(CreateMetricCard("\uE9D9", "ELECTION STATUS", UiTheme.Amber, _electionStateLabel));

        var chartSurface = new RoundedPanel { Dock = DockStyle.Fill, Padding = new Padding(16, 10, 16, 12), BackColor = UiTheme.Surface };
        var chartTitle = new Label
        {
            Text = "Candidate vote distribution",
            Dock = DockStyle.Top,
            Height = 28,
            Font = UiTheme.CreateFont(11.5f, FontStyle.Bold),
            ForeColor = UiTheme.Ink,
        };
        var chartSubtitle = new Label
        {
            Text = "Share of all ballots recorded in this election",
            Dock = DockStyle.Top,
            Height = 20,
            Font = UiTheme.CreateFont(8.5f),
            ForeColor = UiTheme.Muted,
        };
        chartSurface.Controls.Add(_chartViewport);
        chartSurface.Controls.Add(chartSubtitle);
        chartSurface.Controls.Add(chartTitle);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = true,
            BackColor = UiTheme.Canvas,
            Padding = new Padding(0, 4, 0, 0),
        };
        AddActionButton(actions, "\uE710", "Add Candidate", (_, _) => AddCandidate(), UiTheme.Green);
        AddActionButton(actions, "\uE77B", "Manage Voters", (_, _) => ManageVoters(), UiTheme.Success);
        AddActionButton(actions, "\uE8A5", "Candidates", (_, _) => ViewCandidates(), UiTheme.Ocean);
        AddActionButton(actions, "\uE768", "Start Election", (_, _) => StartElection(), UiTheme.Teal);
        AddActionButton(actions, "\uE769", "Pause", (_, _) => PauseElection(), UiTheme.Slate);
        AddActionButton(actions, "\uE711", "Close Election", (_, _) => CloseElection(), UiTheme.Amber);
        AddActionButton(actions, "\uE9D2", "View Results", (_, _) => ViewResults(), UiTheme.Slate);
        AddActionButton(actions, "\uE713", "Settings", (_, _) => EditAdminSettings(), UiTheme.Slate);
        AddActionButton(actions, "\uE8F4", "Send Invitations", (_, _) => SendInvitations(), UiTheme.Ocean);
        AddActionButton(actions, "\uE8BB", "Logout", (_, _) => Logout(), UiTheme.Coral);

        content.Controls.Add(selectorRow, 0, 0);
        content.Controls.Add(metrics, 0, 1);
        content.Controls.Add(chartSurface, 0, 2);
        content.Controls.Add(actions, 0, 3);
        Controls.Add(content);
        Controls.Add(header);
    }

    private static RoundedPanel CreateMetricCard(string glyph, string title, Color accent, Label valueLabel)
    {
        var card = new RoundedPanel
        {
            Width = 230,
            Height = 76,
            Margin = new Padding(0, 0, 8, 0),
            BackColor = UiTheme.Surface,
        };

        var icon = new Label
        {
            Text = glyph,
            Font = new Font("Segoe MDL2 Assets", 13f),
            ForeColor = accent,
            Location = new Point(14, 11),
            Size = new Size(24, 22),
            TextAlign = ContentAlignment.MiddleCenter,
        };

        var label = new Label
        {
            Text = title,
            ForeColor = UiTheme.Muted,
            Font = UiTheme.CreateFont(7.8f, FontStyle.Bold),
            Location = new Point(45, 11),
            AutoSize = true,
        };

        valueLabel.Text = "0";
        valueLabel.ForeColor = UiTheme.Ink;
        valueLabel.Font = UiTheme.CreateFont(18f, FontStyle.Bold);
        valueLabel.Location = new Point(14, 30);
        valueLabel.AutoSize = true;
        card.Controls.Add(icon);
        card.Controls.Add(label);
        card.Controls.Add(valueLabel);
        return card;
    }

    private static void AddActionButton(Control container, string glyph, string text, EventHandler clickHandler, Color color)
    {
        var button = new IconButton(glyph, text)
        {
            Width = 190,
            Height = 42,
            BackColor = color,
            HoverColor = ControlPaint.Light(color, 0.12f),
            Margin = new Padding(0, 0, 9, 8),
        };

        button.Click += clickHandler;
        container.Controls.Add(button);
    }

    private void LoadElections()
    {
        using var context = _databaseFactory.CreateContext();
        var elections = context.Elections.OrderBy(e => e.Title).ToList();

        _electionSelector.Items.Clear();
        foreach (var election in elections)
        {
            _electionSelector.Items.Add(election);
        }

        if (_electionSelector.Items.Count > 0)
        {
            _electionSelector.SelectedIndex = 0;
        }
    }

    private void RefreshAnalytics()
    {
        using var context = _databaseFactory.CreateContext();

        _selectedElection = _electionSelector.SelectedItem as Election;
        int electionId = _selectedElection?.Id ?? 0;

        int totalVoters = context.Voters.Count(v => v.IsActive);
        var electedVoterIds = electionId == 0
            ? new HashSet<int>()
            : context.VoteReceipts.Where(r => r.ElectionId == electionId).Select(r => r.VoterId).Distinct().ToHashSet();
        int votersWhoVoted = electedVoterIds.Count;
        int totalVotes = electionId == 0 ? 0 : context.Ballots.Count(b => b.ElectionId == electionId);
        double turnout = totalVoters == 0 ? 0 : (double)votersWhoVoted / totalVoters;

        var freshElection = electionId == 0 ? null : context.Elections.FirstOrDefault(e => e.Id == electionId);

        _voterCountLabel.Text = totalVoters.ToString("N0");
        _turnoutLabel.Text = turnout.ToString("P0");
        _ballotCountLabel.Text = totalVotes.ToString("N0");
        _electionStateLabel.Text = freshElection?.Status.ToString().ToUpperInvariant() ?? "N/A";

        var candidates = electionId == 0
            ? new List<Candidate>()
            : context.Candidates.Where(c => c.ElectionId == electionId).ToList();
        foreach (var candidate in candidates)
        {
            var count = context.Ballots.Count(b => b.CandidateId == candidate.Id);
            for (int i = 0; i < count; i++)
            {
                candidate.AddVote();
            }
        }

        _voteChart.Candidates = candidates;
        _voteChart.TotalVotes = totalVotes;
        _voteChart.Height = VoteBarChart.PreferredHeight(candidates.Count);
        _voteChart.Invalidate();
    }

    private Election? RequireSelectedElection()
    {
        if (_selectedElection == null)
        {
            MessageBox.Show(this, "Select an election first.", "No Election Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return null;
        }

        return _selectedElection;
    }

    private void AddCandidate()
    {
        var election = RequireSelectedElection();
        if (election == null)
        {
            return;
        }

        var form = new Form
        {
            Text = "Add Candidate",
            Size = new Size(390, 220),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
        };

        var nameLabel = new Label { Text = "Name:", Location = new Point(20, 20), AutoSize = true };
        var nameText = new TextBox { Location = new Point(120, 17), Width = 220 };
        var positionLabel = new Label { Text = "Position:", Location = new Point(20, 55), AutoSize = true };
        var positionText = new TextBox { Location = new Point(120, 52), Width = 220, Text = election.Position };
        var partyLabel = new Label { Text = "Party:", Location = new Point(20, 90), AutoSize = true };
        var partyText = new TextBox { Location = new Point(120, 87), Width = 220 };
        var saveButton = new Button { Text = "Save Candidate", Location = new Point(120, 130), Width = 220, BackColor = UiTheme.Green, ForeColor = Color.White };

        saveButton.Click += (_, _) =>
        {
            string name = nameText.Text.Trim();
            string position = positionText.Text.Trim();
            string party = partyText.Text.Trim();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(position) || string.IsNullOrWhiteSpace(party))
            {
                MessageBox.Show(this, "Candidate details cannot be empty.", "Validation Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var context = _databaseFactory.CreateContext();
            context.Candidates.Add(new Candidate(0, name, position, party) { ElectionId = election.Id });
            context.SaveChanges();
            RefreshAnalytics();

            MessageBox.Show(this, $"Candidate '{name}' added successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            form.Close();
        };

        form.Controls.AddRange(new Control[] { nameLabel, nameText, positionLabel, positionText, partyLabel, partyText, saveButton });
        UiTheme.ApplyDialogStyle(form);
        form.ShowDialog(this);
    }

    private void EditAdminSettings()
    {
        var form = new Form
        {
            Text = "Admin Settings",
            Size = new Size(470, 260),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
        };

        var nameLabel = new Label { Text = "Full name:", Location = new Point(24, 24), AutoSize = true };
        var nameText = new TextBox { Location = new Point(160, 20), Width = 260, Text = _admin.Name };
        var usernameLabel = new Label { Text = "Username:", Location = new Point(24, 66), AutoSize = true };
        var usernameText = new TextBox { Location = new Point(160, 62), Width = 260, Text = _admin.Username };
        var hintLabel = new Label
        {
            Text = "Passwords are stored securely and hidden here.\nUse 'Change Password' to set a new one.",
            Location = new Point(24, 104),
            Size = new Size(400, 40),
            ForeColor = UiTheme.Muted,
        };
        var saveButton = new Button
        {
            Text = "Save Changes",
            Location = new Point(160, 152),
            Width = 130,
            Height = 36,
        };
        var changePasswordButton = new Button
        {
            Text = "Change Password",
            Location = new Point(300, 152),
            Width = 130,
            Height = 36,
        };

        saveButton.Click += (_, _) =>
        {
            string name = nameText.Text.Trim();
            string username = usernameText.Text.Trim();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show(this, "Name and username are required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var context = _databaseFactory.CreateContext();
            bool usernameInUse = context.Admins.Any(admin =>
                admin.Id != _admin.Id &&
                admin.Username == username);
            if (usernameInUse)
            {
                MessageBox.Show(this, "That username is already in use.", "Settings Not Saved", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var tracked = context.Admins.First(a => a.Id == _admin.Id);
            tracked.Name = name;
            tracked.Username = username;
            context.SaveChanges();

            _admin.Name = name;
            _admin.Username = username;
            MessageBox.Show(this, "Admin settings updated.", "Settings Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            form.Close();
        };

        changePasswordButton.Click += (_, _) =>
        {
            using var dialog = new ChangePasswordDialog(_admin.Id, _databaseFactory);
            dialog.ShowDialog(this);
        };

        form.Controls.AddRange(new Control[] { nameLabel, nameText, usernameLabel, usernameText, hintLabel, saveButton, changePasswordButton });
        UiTheme.ApplyDialogStyle(form);
        form.ShowDialog(this);
    }

    public void RequestBack() => Logout();

    private void Logout()
    {
        _autoRefreshTimer.Stop();
        _navigation.Reset(() => new MainForm(_databaseFactory, _navigation));
    }

    private void ManageVoters()
    {
        _navigation.Navigate(() => new VoterManagementForm(_databaseFactory, _navigation));
        RefreshAnalytics();
    }

    private void ViewCandidates()
    {
        _navigation.Navigate(() => new CandidateListForm(_databaseFactory, _navigation));
    }

    private void SendInvitations()
    {
        _navigation.Navigate(() => new SendInvitationsForm(_databaseFactory, _navigation));
    }

    private void StartElection()
    {
        var election = RequireSelectedElection();
        if (election == null)
        {
            return;
        }

        var confirm = MessageBox.Show(
            this,
            $"Start voting for '{election.Title}'?\n\nThis validates the configuration, opens voting, and records who started it.",
            "Confirm Start Election",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        var result = _electionService.StartElection(election.Id, _admin.Id);
        MessageBox.Show(
            this,
            result.Message,
            result.Success ? "Election Started" : "Cannot Start Election",
            MessageBoxButtons.OK,
            result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

        LoadElections();
        RefreshAnalytics();
    }

    private void PauseElection()
    {
        var election = RequireSelectedElection();
        if (election == null)
        {
            return;
        }

        var result = _electionService.PauseElection(election.Id);
        MessageBox.Show(
            this,
            result.Message,
            result.Success ? "Election Paused" : "Cannot Pause",
            MessageBoxButtons.OK,
            result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

        LoadElections();
        RefreshAnalytics();
    }

    private void CloseElection()
    {
        var election = RequireSelectedElection();
        if (election == null)
        {
            return;
        }

        var confirm = MessageBox.Show(
            this,
            $"Close voting for '{election.Title}'?\n\nA closed election cannot be reopened.",
            "Confirm Close Election",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        var result = _electionService.CloseElection(election.Id);
        MessageBox.Show(
            this,
            result.Message,
            result.Success ? "Election Closed" : "Cannot Close",
            MessageBoxButtons.OK,
            result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

        LoadElections();
        RefreshAnalytics();
    }

    private void ViewResults()
    {
        var election = RequireSelectedElection();
        if (election == null)
        {
            return;
        }

        using var context = _databaseFactory.CreateContext();
        var candidates = context.Candidates.Where(c => c.ElectionId == election.Id).ToList();

        if (candidates.Count == 0)
        {
            MessageBox.Show(this, "No candidates have been added to this election.", "Election Results", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var lines = candidates
            .Select(c => new
            {
                c.Name,
                Votes = context.Ballots.Count(b => b.CandidateId == c.Id),
            })
            .OrderByDescending(x => x.Votes)
            .Select(x => $"{x.Name}: {x.Votes} vote(s)")
            .ToList();

        MessageBox.Show(this, string.Join(Environment.NewLine, lines), "Election Results", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
