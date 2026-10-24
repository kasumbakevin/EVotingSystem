using EVotingSystem.Data;
using EVotingSystem.Models;
using EVotingSystem.Services;

namespace EVotingSystem.Forms;

/// <summary>
/// Full voter register management: search, register, edit, verify, deactivate
/// and reinstate. Verification is what makes a voter eligible to cast a ballot.
/// </summary>
public class VoterManagementForm : Form, INavigationAware
{
    private readonly VoterService _voterService;
    private readonly FormNavigationService _navigation;
    private readonly DatabaseFactory _databaseFactory;
    private readonly DataGridView _grid = new();
    private readonly TextBox _searchBox = new();
    private readonly ComboBox _statusFilter = new();
    private readonly Label _countLabel = new();

    public VoterManagementForm(DatabaseFactory databaseFactory, FormNavigationService navigation)
    {
        _voterService = new VoterService(databaseFactory);
        _navigation = navigation;
        _databaseFactory = databaseFactory;

        Text = "Voter Management";
        Size = new Size(1100, 720);
        MinimumSize = new Size(900, 600);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;
        BackColor = UiTheme.Canvas;

        BuildInterface();
        RefreshGrid();
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
            Text = "VOTER MANAGEMENT",
            Dock = DockStyle.Top,
            Height = 36,
            Font = UiTheme.CreateFont(18f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        var subtitle = new Label
        {
            Text = "Registration, verification, eligibility and eligibility status",
            Dock = DockStyle.Fill,
            Font = UiTheme.CreateFont(9.5f),
            ForeColor = UiTheme.HeaderSubtext,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        header.Controls.Add(subtitle);
        header.Controls.Add(title);

        var toolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 62,
            BackColor = UiTheme.Canvas,
            Padding = new Padding(20, 14, 20, 8),
        };

        var searchLabel = new Label { Text = "Search:", AutoSize = true, Location = new Point(20, 22), ForeColor = UiTheme.Ink, Font = UiTheme.CreateFont(9.5f, FontStyle.Bold) };
        _searchBox.Location = new Point(80, 18);
        _searchBox.Width = 300;
        _searchBox.Font = UiTheme.CreateFont(10f);
        _searchBox.BorderStyle = BorderStyle.FixedSingle;
        _searchBox.TextChanged += (_, _) => RefreshGrid();

        var filterLabel = new Label { Text = "Status:", AutoSize = true, Location = new Point(400, 22), ForeColor = UiTheme.Ink, Font = UiTheme.CreateFont(9.5f, FontStyle.Bold) };
        _statusFilter.Location = new Point(456, 18);
        _statusFilter.Width = 170;
        _statusFilter.DropDownStyle = ComboBoxStyle.DropDownList;
        _statusFilter.Font = UiTheme.CreateFont(10f);
        _statusFilter.Items.AddRange(new object[] { "All", "Pending Verification", "Verified", "Manual Review", "Rejected", "Inactive" });
        _statusFilter.SelectedIndex = 0;
        _statusFilter.SelectedIndexChanged += (_, _) => RefreshGrid();

        _countLabel.AutoSize = true;
        _countLabel.Location = new Point(650, 22);
        _countLabel.ForeColor = UiTheme.Muted;
        _countLabel.Font = UiTheme.CreateFont(9.5f);

        toolbar.Controls.AddRange(new Control[] { searchLabel, _searchBox, filterLabel, _statusFilter, _countLabel });

        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AutoGenerateColumns = false;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "Id", DataPropertyName = "Id", HeaderText = "ID", FillWeight = 35, MinimumWidth = 45 },
            new DataGridViewTextBoxColumn { Name = "Name", DataPropertyName = "Name", HeaderText = "Full Name", FillWeight = 150, MinimumWidth = 120 },
            new DataGridViewTextBoxColumn { Name = "VoterNumber", DataPropertyName = "VoterNumber", HeaderText = "Voter Number", FillWeight = 100, MinimumWidth = 90 },
            new DataGridViewTextBoxColumn { Name = "Username", DataPropertyName = "Username", HeaderText = "Username", FillWeight = 100, MinimumWidth = 90 },
            new DataGridViewTextBoxColumn { Name = "Department", DataPropertyName = "Department", HeaderText = "Department", FillWeight = 110, MinimumWidth = 90 },
            new DataGridViewTextBoxColumn { Name = "VerificationStatus", DataPropertyName = "VerificationStatus", HeaderText = "Verification", FillWeight = 120, MinimumWidth = 100 },
            new DataGridViewTextBoxColumn { Name = "ActiveText", DataPropertyName = "ActiveText", HeaderText = "Active", FillWeight = 55, MinimumWidth = 60 },
            new DataGridViewTextBoxColumn { Name = "HasVoted", DataPropertyName = "HasVoted", HeaderText = "Voted", FillWeight = 55, MinimumWidth = 60 }
        );
        UiTheme.StyleGrid(_grid);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 96,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = UiTheme.Canvas,
            Padding = new Padding(18, 10, 18, 10),
        };
        AddAction(actions, "\uE710", "Register Voter", (_, _) => RegisterVoter(), UiTheme.Green);
        AddAction(actions, "\uE70F", "Edit Voter", (_, _) => EditVoter(), UiTheme.Ocean);
        AddAction(actions, "\uE8FB", "Verify", (_, _) => SetVerification(VoterVerificationStatus.Verified), UiTheme.Success);
        AddAction(actions, "\uE8B7", "Face Scan", (_, _) => OpenFaceVerification(), UiTheme.Teal);
        AddAction(actions, "\uE7BA", "Manual Review", (_, _) => SetVerification(VoterVerificationStatus.ManualReview), UiTheme.Amber);
        AddAction(actions, "\uE711", "Reject", (_, _) => SetVerification(VoterVerificationStatus.Rejected), UiTheme.Coral);
        AddAction(actions, "\uE74D", "Deactivate", (_, _) => SetActive(false), UiTheme.Slate);
        AddAction(actions, "\uE777", "Reinstate", (_, _) => SetActive(true), UiTheme.Teal);
        AddAction(actions, "\uE72E", "Reset Password", (_, _) => ResetPassword(), UiTheme.Slate);
        AddAction(actions, "\uE72C", "Refresh", (_, _) => RefreshGrid(), UiTheme.Ocean);
        AddAction(actions, "\uE72B", "Back to Menu", (_, _) => _navigation.Back(), UiTheme.Coral);

        Controls.Add(_grid);
        Controls.Add(toolbar);
        Controls.Add(actions);
        Controls.Add(header);
    }

    private static void AddAction(Control container, string glyph, string text, EventHandler handler, Color color)
    {
        var button = new IconButton(glyph, text)
        {
            Width = 165,
            Height = 40,
            BackColor = color,
            HoverColor = ControlPaint.Light(color, 0.12f),
            Margin = new Padding(0, 0, 8, 8),
        };
        button.Click += handler;
        container.Controls.Add(button);
    }

    private void RefreshGrid()
    {
        var voters = _voterService.Search(_searchBox.Text);

        var filter = _statusFilter.SelectedItem as string ?? "All";
        voters = filter switch
        {
            "Pending Verification" => voters.Where(v => v.VerificationStatus == VoterVerificationStatus.PendingVerification).ToList(),
            "Verified" => voters.Where(v => v.VerificationStatus == VoterVerificationStatus.Verified).ToList(),
            "Manual Review" => voters.Where(v => v.VerificationStatus == VoterVerificationStatus.ManualReview).ToList(),
            "Rejected" => voters.Where(v => v.VerificationStatus == VoterVerificationStatus.Rejected).ToList(),
            "Inactive" => voters.Where(v => !v.IsActive).ToList(),
            _ => voters,
        };

        _grid.DataSource = voters
            .Select(v => new
            {
                v.Id,
                v.Name,
                v.VoterNumber,
                v.Username,
                Department = v.Department ?? "-",
                VerificationStatus = v.VerificationStatus.ToString(),
                ActiveText = v.IsActive ? "Yes" : "No",
                v.HasVoted,
            })
            .ToList();

        _countLabel.Text = $"{voters.Count} voter(s) shown";
    }

    private Voter? SelectedVoter()
    {
        if (_grid.CurrentRow?.DataBoundItem == null)
        {
            MessageBox.Show(this, "Select a voter first.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return null;
        }

        var idValue = _grid.CurrentRow.Cells["Id"].Value;
        if (idValue is not int id)
        {
            MessageBox.Show(this, "Select a voter first.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return null;
        }

        return _voterService.GetAll().FirstOrDefault(v => v.Id == id);
    }

    private void RegisterVoter()
    {
        using var dialog = new VoterEditDialog(_voterService, null);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            RefreshGrid();
        }
    }

    private void EditVoter()
    {
        var voter = SelectedVoter();
        if (voter == null)
        {
            return;
        }

        using var dialog = new VoterEditDialog(_voterService, voter);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            RefreshGrid();
        }
    }

    private void SetVerification(VoterVerificationStatus status)
    {
        var voter = SelectedVoter();
        if (voter == null)
        {
            return;
        }

        var result = _voterService.SetVerification(voter.Id, status);
        ShowResult(result);
        RefreshGrid();
    }

    private void OpenFaceVerification()
    {
        var voter = SelectedVoter();
        if (voter == null)
        {
            return;
        }

        _navigation.Navigate(() => new FaceVerificationForm(voter, _databaseFactory, _navigation));
        RefreshGrid();
    }

    private void SetActive(bool active)
    {
        var voter = SelectedVoter();
        if (voter == null)
        {
            return;
        }

        var result = _voterService.SetActive(voter.Id, active);
        ShowResult(result);
        RefreshGrid();
    }

    private void ResetPassword()
    {
        var voter = SelectedVoter();
        if (voter == null)
        {
            return;
        }

        var form = new Form
        {
            Text = "Reset Password",
            Size = new Size(420, 170),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
        };

        var label = new Label { Text = "New password:", Location = new Point(20, 24), AutoSize = true };
        var text = new TextBox { Location = new Point(150, 20), Width = 230, UseSystemPasswordChar = true };
        var save = new Button { Text = "Reset", Location = new Point(150, 66), Width = 230, Height = 36 };

        save.Click += (_, _) =>
        {
            var result = _voterService.ResetPassword(voter.Id, text.Text);
            ShowResult(result);
            if (result.Success)
            {
                form.Close();
            }
        };

        form.Controls.AddRange(new Control[] { label, text, save });
        UiTheme.ApplyDialogStyle(form);
        form.ShowDialog(this);
    }

    private void ShowResult(ServiceResult result)
    {
        MessageBox.Show(
            this,
            result.Message,
            result.Success ? "Success" : "Action Not Completed",
            MessageBoxButtons.OK,
            result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
    }
}
