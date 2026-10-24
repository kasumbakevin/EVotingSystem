using EVotingSystem.Data;
using EVotingSystem.Models;
using EVotingSystem.Services;

namespace EVotingSystem.Forms;

/// <summary>
/// Reports and published results. Results are only shown for elections that have
/// been closed, so in-progress tallies stay confidential. A report generation
/// action records an audit entry and writes a CSV summary.
/// </summary>
public class ReportsForm : Form, INavigationAware
{
    private readonly DatabaseFactory _databaseFactory;
    private readonly FormNavigationService _navigation;
    private readonly AuditService _auditService;
    private readonly ComboBox _electionSelector = new();
    private readonly DataGridView _grid = new();
    private readonly Label _statusLabel = new();

    public ReportsForm(DatabaseFactory databaseFactory, FormNavigationService navigation)
    {
        _databaseFactory = databaseFactory;
        _navigation = navigation;
        _auditService = new AuditService(databaseFactory);

        Text = "Reports";
        Size = new Size(940, 680);
        MinimumSize = new Size(780, 540);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        BackColor = UiTheme.Canvas;

        BuildInterface();
        LoadElections();
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
            Text = "REPORTS & RESULTS",
            Dock = DockStyle.Top,
            Height = 36,
            Font = UiTheme.CreateFont(18f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        var subtitle = new Label
        {
            Text = "Published tallies for closed elections and downloadable summaries",
            Dock = DockStyle.Fill,
            Font = UiTheme.CreateFont(9.5f),
            ForeColor = UiTheme.HeaderSubtext,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        header.Controls.Add(subtitle);
        header.Controls.Add(title);

        var toolbar = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = UiTheme.Canvas, Padding = new Padding(20, 14, 20, 6) };
        var label = new Label { Text = "Election:", AutoSize = true, Location = new Point(20, 20), ForeColor = UiTheme.Ink, Font = UiTheme.CreateFont(9.5f, FontStyle.Bold) };
        _electionSelector.Location = new Point(90, 16);
        _electionSelector.Width = 420;
        _electionSelector.DropDownStyle = ComboBoxStyle.DropDownList;
        _electionSelector.Font = UiTheme.CreateFont(10f);
        _electionSelector.SelectedIndexChanged += (_, _) => LoadResults();
        toolbar.Controls.Add(label);
        toolbar.Controls.Add(_electionSelector);

        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AutoGenerateColumns = false;
        _grid.AllowUserToAddRows = false;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "Rank", HeaderText = "#", FillWeight = 35, MinimumWidth = 40 },
            new DataGridViewTextBoxColumn { Name = "Candidate", HeaderText = "Candidate", FillWeight = 150, MinimumWidth = 120 },
            new DataGridViewTextBoxColumn { Name = "Party", HeaderText = "Party", FillWeight = 130, MinimumWidth = 110 },
            new DataGridViewTextBoxColumn { Name = "Votes", HeaderText = "Votes", FillWeight = 70, MinimumWidth = 70 },
            new DataGridViewTextBoxColumn { Name = "Share", HeaderText = "Share", FillWeight = 70, MinimumWidth = 70 });
        UiTheme.StyleGrid(_grid);

        var gridHost = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Canvas, Padding = new Padding(20, 8, 20, 8) };
        gridHost.Controls.Add(_grid);

        _statusLabel.Dock = DockStyle.Bottom;
        _statusLabel.Height = 40;
        _statusLabel.Padding = new Padding(22, 0, 22, 0);
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusLabel.Font = UiTheme.CreateFont(9.5f);
        _statusLabel.ForeColor = UiTheme.Ink;
        _statusLabel.BackColor = UiTheme.Mint;

        var actions = new Panel { Dock = DockStyle.Bottom, Height = 62, BackColor = UiTheme.Canvas, Padding = new Padding(20, 10, 20, 10) };
        var export = new IconButton("\uE896", "Generate Report")
        {
            Dock = DockStyle.Left,
            Width = 210,
            BackColor = UiTheme.Green,
            HoverColor = ControlPaint.Light(UiTheme.Green, 0.12f),
        };
        export.Click += (_, _) => GenerateReport();
        var back = new IconButton("\uE72B", "Back")
        {
            Dock = DockStyle.Right,
            Width = 160,
            BackColor = UiTheme.Coral,
            HoverColor = ControlPaint.Light(UiTheme.Coral, 0.12f),
        };
        back.Click += (_, _) => _navigation.Back();
        actions.Controls.Add(back);
        actions.Controls.Add(export);

        Controls.Add(gridHost);
        Controls.Add(actions);
        Controls.Add(_statusLabel);
        Controls.Add(toolbar);
        Controls.Add(header);
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
        else
        {
            _statusLabel.Text = "No elections have been configured yet.";
        }
    }

    private void LoadResults()
    {
        if (_electionSelector.SelectedItem is not Election election)
        {
            return;
        }

        if (election.Status != ElectionStatus.Closed && election.Status != ElectionStatus.Archived)
        {
            _grid.DataSource = null;
            _statusLabel.Text = $"Results for '{election.Title}' are confidential until the election is closed. Current status: {election.Status}.";
            return;
        }

        using var context = _databaseFactory.CreateContext();
        var candidates = context.Candidates.Where(c => c.ElectionId == election.Id).ToList();
        int total = candidates.Sum(c => context.Ballots.Count(b => b.CandidateId == c.Id));

        var rows = candidates
            .Select(c => new
            {
                Candidate = c.Name,
                c.Party,
                Votes = context.Ballots.Count(b => b.CandidateId == c.Id),
            })
            .OrderByDescending(r => r.Votes)
            .Select((r, index) => new
            {
                Rank = index + 1,
                r.Candidate,
                r.Party,
                r.Votes,
                Share = total == 0 ? "0%" : $"{(double)r.Votes / total:P0}",
            })
            .ToList();

        _grid.DataSource = rows;
        _statusLabel.Text = $"'{election.Title}' - {total:N0} ballot(s) recorded.  Status: {election.Status}.";
    }

    private void GenerateReport()
    {
        if (_electionSelector.SelectedItem is not Election election)
        {
            MessageBox.Show(this, "Select an election first.", "No Election Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (election.Status != ElectionStatus.Closed && election.Status != ElectionStatus.Archived)
        {
            MessageBox.Show(this, "Reports can only be generated for closed elections.", "Not Available", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var context = _databaseFactory.CreateContext();
        var candidates = context.Candidates.Where(c => c.ElectionId == election.Id).ToList();
        int total = candidates.Sum(c => context.Ballots.Count(b => b.CandidateId == c.Id));

        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EVotingSystem",
            "reports");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"results_{election.Id}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");

        using (var writer = new StreamWriter(path))
        {
            writer.WriteLine("Election,Position,Candidate,Party,Votes,Share");
            var ordered = candidates
                .Select(c => new { c.Name, c.Party, Votes = context.Ballots.Count(b => b.CandidateId == c.Id) })
                .OrderByDescending(x => x.Votes);
            foreach (var row in ordered)
            {
                string share = total == 0 ? "0%" : $"{(double)row.Votes / total:P1}";
                writer.WriteLine($"\"{election.Title}\",\"{election.Position}\",\"{row.Name}\",\"{row.Party}\",{row.Votes},\"{share}\"");
            }
        }

        _auditService.Record(AuditCategories.Report, "Report generated", $"Results report for '{election.Title}'.", "Admin", null, election.Id);
        MessageBox.Show(this, $"Report written to:\n{path}", "Report Generated", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
