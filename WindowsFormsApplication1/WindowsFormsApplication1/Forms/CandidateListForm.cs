using EVotingSystem.Data;

namespace EVotingSystem.Forms;

public class CandidateListForm : Form, INavigationAware
{
    private readonly FormNavigationService _navigation;

    public CandidateListForm(DatabaseFactory databaseFactory, FormNavigationService navigation)
    {
        _navigation = navigation;

        this.Text = "Candidate List";
        this.Size = new Size(900, 560);
        this.MinimumSize = new Size(760, 420);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.MaximizeBox = true;
        this.MinimizeBox = false;
        this.BackColor = UiTheme.Canvas;

        var titleBar = new GradientPanel
        {
            Dock = DockStyle.Top,
            Height = 64,
            StartColor = UiTheme.HeaderStart,
            EndColor = UiTheme.HeaderEnd,
        };

        var titleLabel = new Label
        {
            Text = "CANDIDATES",
            Font = UiTheme.CreateFont(18f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
        };

        titleBar.Controls.Add(titleLabel);

        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        };
        grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "Id", DataPropertyName = "Id", HeaderText = "ID", FillWeight = 45, MinimumWidth = 55 },
            new DataGridViewTextBoxColumn { Name = "Name", DataPropertyName = "Name", HeaderText = "Candidate", FillWeight = 150, MinimumWidth = 120 },
            new DataGridViewTextBoxColumn { Name = "Position", DataPropertyName = "Position", HeaderText = "Position", FillWeight = 145, MinimumWidth = 120 },
            new DataGridViewTextBoxColumn { Name = "Party", DataPropertyName = "Party", HeaderText = "Party", FillWeight = 145, MinimumWidth = 150 },
            new DataGridViewTextBoxColumn { Name = "Votes", DataPropertyName = "Votes", HeaderText = "Votes", FillWeight = 60, MinimumWidth = 70 }
        );
        UiTheme.StyleGrid(grid);

        using var context = databaseFactory.CreateContext();
        var candidates = context.Candidates.OrderBy(c => c.Id).ToList();
        grid.DataSource = candidates
            .Select(candidate => new
            {
                candidate.Id,
                candidate.Name,
                candidate.Position,
                candidate.Party,
                Votes = context.Ballots.Count(b => b.CandidateId == candidate.Id),
            })
            .ToList();

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 64,
            BackColor = UiTheme.Canvas,
            Padding = new Padding(18, 12, 18, 10),
        };
        var backButton = new IconButton("\uE72B", "Back")
        {
            Width = 160,
            Height = 40,
            BackColor = UiTheme.Coral,
            HoverColor = ControlPaint.Light(UiTheme.Coral, 0.12f),
        };
        backButton.Click += (_, _) => _navigation.Back();
        actions.Controls.Add(backButton);

        this.Controls.Add(grid);
        this.Controls.Add(actions);
        this.Controls.Add(titleBar);
    }

    public void RequestBack() => _navigation.Back();
}
