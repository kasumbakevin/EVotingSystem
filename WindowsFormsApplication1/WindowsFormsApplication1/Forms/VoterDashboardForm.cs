using EVotingSystem.Data;
using EVotingSystem.Models;
using EVotingSystem.Services;

namespace EVotingSystem.Forms;

public class VoterDashboardForm : Form, INavigationAware
{
    private readonly Voter _voter;
    private readonly VotingService _votingService;
    private readonly DatabaseFactory _databaseFactory;
    private readonly FormNavigationService _navigation;
    private readonly Label _votingStateLabel = new();
    private readonly Label _electionStatusLabel = new();
    private readonly DataGridView _grid = new();
    private readonly ComboBox _candidateSelector = new();

    public VoterDashboardForm(Voter voter, VotingService votingService, DatabaseFactory databaseFactory, FormNavigationService navigation)
    {
        _voter = voter;
        _votingService = votingService;
        _databaseFactory = databaseFactory;
        _navigation = navigation;

        this.Text = "Voter Dashboard";
        this.Size = new Size(920, 700);
        this.MinimumSize = new Size(820, 620);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.MaximizeBox = true;
        this.MinimizeBox = true;
        this.BackColor = UiTheme.Canvas;

        InitializeInterface();
        RefreshStatusCards();
        RefreshCandidateGrid();

        Activated += (_, _) =>
        {
            RefreshStatusCards();
            RefreshCandidateGrid();
        };
    }

    public void RequestBack() => Logout();

    private void Logout()
    {
        _navigation.Reset(() => new MainForm(_databaseFactory, _navigation));
    }

    private void InitializeInterface()
    {
        var header = new GradientPanel
        {
            Dock = DockStyle.Top,
            Height = 100,
            Padding = new Padding(24, 14, 24, 12),
            StartColor = UiTheme.HeaderStart,
            EndColor = UiTheme.HeaderEnd,
        };

        var title = new Label
        {
            Text = $"WELCOME, {_voter.Name.ToUpperInvariant()}",
            Font = UiTheme.CreateFont(18f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
        };

        header.Controls.Add(title);

        var infoPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 92,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(20, 10, 20, 10),
            BackColor = UiTheme.Canvas,
        };
        infoPanel.SizeChanged += (_, _) =>
        {
            int cardWidth = Math.Max(210, (infoPanel.ClientSize.Width - infoPanel.Padding.Horizontal - 28) / 3);
            foreach (Control control in infoPanel.Controls)
            {
                control.Width = cardWidth;
            }
        };

        infoPanel.Controls.Add(CreateInfoCard("\uE77B", "Voter Number", _voter.VoterNumber, UiTheme.Green));
        infoPanel.Controls.Add(CreateInfoCard("\uE9D9", "Election Status", "-", UiTheme.Ocean, _electionStatusLabel));
        infoPanel.Controls.Add(CreateInfoCard("\uE8D7", "Voting State", _voter.HasVoted ? "VOTED" : "PENDING", UiTheme.Amber, _votingStateLabel));

        _grid.Dock = DockStyle.Top;
        _grid.Height = 320;
        _grid.ReadOnly = true;
        _grid.AutoGenerateColumns = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        UiTheme.StyleGrid(_grid);

        var votePanel = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 12, 20, 16),
            Margin = new Padding(20, 8, 20, 18),
            BackColor = UiTheme.Surface,
        };

        var ballotTitle = new Label
        {
            Text = "YOUR BALLOT",
            Location = new Point(20, 12),
            AutoSize = true,
            Font = UiTheme.CreateFont(10f, FontStyle.Bold),
            ForeColor = UiTheme.Muted,
        };
        var candidateLabel = new Label { Text = "Select candidate", AutoSize = true, Location = new Point(20, 46), Font = UiTheme.CreateFont(10f, FontStyle.Bold), ForeColor = UiTheme.Ink };
        _candidateSelector.Location = new Point(20, 68);
        _candidateSelector.Width = 520;
        _candidateSelector.Height = 32;
        _candidateSelector.DropDownStyle = ComboBoxStyle.DropDownList;
        _candidateSelector.Font = UiTheme.CreateFont(10.5f);
        _candidateSelector.Anchor = AnchorStyles.Top | AnchorStyles.Left;

        var voteButton = new IconButton("\uE8D7", "Cast Vote")
        {
            Location = new Point(560, 64),
            Width = 180,
            Height = 40,
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
        };

        voteButton.Click += (_, _) =>
        {
            if (_candidateSelector.SelectedItem is not CandidateOption option)
            {
                MessageBox.Show(this, "Please select a candidate.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = _votingService.CastVote(_voter.Id, option.Id);

            MessageBox.Show(
                this,
                result.Message,
                result.Success ? "Success" : "Vote Not Cast",
                MessageBoxButtons.OK,
                result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

            if (result.Success)
            {
                RefreshStatusCards();
                RefreshCandidateGrid();
            }
        };

        var logoutButton = new IconButton("\uE8BB", "Logout")
        {
            Location = new Point(20, 140),
            Width = 180,
            Height = 40,
            BackColor = UiTheme.Coral,
            HoverColor = ControlPaint.Light(UiTheme.Coral, 0.12f),
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
        };
        logoutButton.Click += (_, _) => Logout();

        votePanel.Controls.Add(ballotTitle);
        votePanel.Controls.Add(candidateLabel);
        votePanel.Controls.Add(_candidateSelector);
        votePanel.Controls.Add(voteButton);
        votePanel.Controls.Add(logoutButton);

        this.Controls.Add(votePanel);
        this.Controls.Add(_grid);
        this.Controls.Add(infoPanel);
        this.Controls.Add(header);
    }

    private void RefreshStatusCards()
    {
        using var context = _databaseFactory.CreateContext();
        var openElections = context.Elections.Where(e => e.Status == ElectionStatus.Open).ToList();
        _electionStatusLabel.Text = openElections.Count == 0 ? "CLOSED" : "OPEN";

        var freshVoter = context.Voters.FirstOrDefault(v => v.Id == _voter.Id);
        _votingStateLabel.Text = freshVoter?.HasVoted == true ? "VOTED" : "PENDING";
    }

    private void RefreshCandidateGrid()
    {
        using var context = _databaseFactory.CreateContext();
        var candidates = context.Candidates.OrderBy(c => c.Id).ToList();

        _grid.DataSource = candidates
            .Select(candidate => new
            {
                candidate.Id,
                candidate.Name,
                candidate.Position,
                candidate.Party,
                Votes = context.Ballots.Count(b => b.CandidateId == candidate.Id),
            })
            .ToList();

        _candidateSelector.Items.Clear();
        foreach (var candidate in candidates)
        {
            _candidateSelector.Items.Add(new CandidateOption(candidate.Id, $"{candidate.Name} ({candidate.Party})"));
        }
    }

    private sealed record CandidateOption(int Id, string Display)
    {
        public override string ToString() => Display;
    }

    private static RoundedPanel CreateInfoCard(string glyph, string label, string value, Color accent, Label? valueLabel = null)
    {
        var card = new RoundedPanel
        {
            Width = 260,
            Height = 72,
            BackColor = UiTheme.Surface,
            Margin = new Padding(0, 0, 10, 0),
        };

        var icon = new Label
        {
            Text = glyph,
            ForeColor = accent,
            Font = new Font("Segoe MDL2 Assets", 13.5f),
            Location = new Point(12, 14),
            Size = new Size(24, 24),
            TextAlign = ContentAlignment.MiddleCenter,
        };

        var labelText = new Label
        {
            Text = label,
            ForeColor = UiTheme.Muted,
            Font = UiTheme.CreateFont(8.5f),
            AutoSize = true,
            Location = new Point(46, 11),
        };

        valueLabel ??= new Label();
        valueLabel.Text = value;
        valueLabel.ForeColor = UiTheme.Ink;
        valueLabel.Font = UiTheme.CreateFont(13.5f, FontStyle.Bold);
        valueLabel.AutoSize = true;
        valueLabel.Location = new Point(46, 29);

        card.Controls.Add(icon);
        card.Controls.Add(labelText);
        card.Controls.Add(valueLabel);
        return card;
    }
}
