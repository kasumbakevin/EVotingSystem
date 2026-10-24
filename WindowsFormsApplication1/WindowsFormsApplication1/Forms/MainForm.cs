using EVotingSystem.Data;
using EVotingSystem.Models;
using EVotingSystem.Services;

namespace EVotingSystem.Forms;

public class MainForm : Form, INavigationAware
{
    private readonly DatabaseFactory _databaseFactory;
    private readonly FormNavigationService _navigation;
    private readonly AuthenticationService _authenticationService;
    private readonly VotingService _votingService;
    private readonly ElectionService _electionService;
    private readonly AuditService _auditService;

    public MainForm(DatabaseFactory databaseFactory, FormNavigationService navigation)
    {
        _databaseFactory = databaseFactory;
        _navigation = navigation;
        _authenticationService = new AuthenticationService(databaseFactory);
        _votingService = new VotingService(databaseFactory);
        _electionService = new ElectionService(databaseFactory);
        _auditService = new AuditService(databaseFactory);

        Text = "E-Voting System";
        Size = new Size(760, 760);
        MinimumSize = new Size(680, 660);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = true;
        BackColor = UiTheme.Canvas;

        InitializeInterface();
    }

    public void RequestBack()
    {
        // The main menu is the root; there is nowhere further back.
    }

    private void InitializeInterface()
    {
        var header = new GradientPanel
        {
            Dock = DockStyle.Top,
            Height = 156,
            Padding = new Padding(30, 22, 30, 16),
            StartColor = UiTheme.HeaderStart,
            EndColor = UiTheme.HeaderEnd,
        };

        var title = new Label
        {
            Text = "E-VOTING SYSTEM",
            Font = UiTheme.CreateFont(24f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Dock = DockStyle.Top,
            Height = 44,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        var subtitle = new Label
        {
            Text = "Secure digital voting for your institution",
            Font = UiTheme.CreateFont(10f),
            ForeColor = UiTheme.HeaderSubtext,
            BackColor = Color.Transparent,
            Dock = DockStyle.Top,
            Height = 26,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        var statusLine = new Label
        {
            Text = BuildStatusLine(),
            Font = UiTheme.CreateFont(8.5f, FontStyle.Bold),
            ForeColor = UiTheme.Mint,
            BackColor = Color.Transparent,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft,
        };

        header.Controls.Add(statusLine);
        header.Controls.Add(subtitle);
        header.Controls.Add(title);

        var container = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.Canvas,
            Padding = new Padding(40, 26, 40, 30),
        };

        var menuSurface = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 18, 24, 18),
            BackColor = UiTheme.Surface,
        };

        var menuTitle = new Label
        {
            Text = "MAIN MENU",
            Dock = DockStyle.Top,
            Height = 30,
            Font = UiTheme.CreateFont(9f, FontStyle.Bold),
            ForeColor = UiTheme.Muted,
        };

        var menuPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0, 4, 0, 0),
            BackColor = UiTheme.Surface,
            AutoScroll = true,
        };
        menuPanel.SizeChanged += (_, _) => ResizeMenuButtons(menuPanel);

        AddMenuButton(menuPanel, "\uE7EF", "Admin Login", "Sign in as an election administrator", HandleAdminLogin, UiTheme.Green);
        AddMenuButton(menuPanel, "\uE8FA", "Voter Registration", "Register a voter and verify identity", HandleVoterRegistration, UiTheme.Success);
        AddMenuButton(menuPanel, "\uE8B7", "Voter Login", "Vote using your voter credentials", HandleVoterLogin, UiTheme.Teal);
        AddMenuButton(menuPanel, "\uE8A5", "View Candidates", "Browse candidates running in elections", HandleViewCandidates, UiTheme.Ocean);
        AddMenuButton(menuPanel, "\uE9D9", "View Election Status", "See the current state of each election", HandleElectionStatus, UiTheme.Amber);
        AddMenuButton(menuPanel, "\uE9D2", "Reports", "Published results and turnout reports", HandleReports, UiTheme.Slate);
        AddMenuButton(menuPanel, "\uE713", "Settings", "Application and email configuration", HandleSettings, UiTheme.Slate);
        AddMenuButton(menuPanel, "\uE8BB", "Exit", "Close the application", (_, _) => _navigation.ExitApplication(), UiTheme.Coral);

        menuSurface.Controls.Add(menuPanel);
        menuSurface.Controls.Add(menuTitle);
        container.Controls.Add(menuSurface);
        Controls.Add(container);
        Controls.Add(header);
    }

    private string BuildStatusLine()
    {
        using var context = _databaseFactory.CreateContext();
        int elections = context.Elections.Count();
        int open = context.Elections.Count(e => e.Status == ElectionStatus.Open);
        return $"{elections} election(s) configured  |  {open} open for voting";
    }

    private static void ResizeMenuButtons(FlowLayoutPanel panel)
    {
        int width = Math.Max(260, panel.ClientSize.Width - panel.Padding.Horizontal - 6);
        foreach (Control control in panel.Controls)
        {
            control.Width = width;
        }
    }

    private static void AddMenuButton(Control container, string glyph, string text, string description, EventHandler clickHandler, Color color)
    {
        var button = new MenuActionButton(glyph, text, description, color)
        {
            Width = 520,
            Height = 58,
            Margin = new Padding(0, 5, 0, 5),
        };

        button.Click += clickHandler;
        container.Controls.Add(button);
    }

    private void HandleAdminLogin(object? sender, EventArgs e)
    {
        using var loginForm = new LoginDialog("Admin Login", "Administrator access", isAdmin: true);
        loginForm.SetExtraHint("Default first-run account: admin / ChangeMe123!");
        UiTheme.ApplyDialogStyle(loginForm);

        if (loginForm.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var admin = _authenticationService.AdminLogin(loginForm.Username, loginForm.Password);
        if (admin == null)
        {
            _auditService.Record(AuditCategories.Security, "Admin login failed", $"Username '{loginForm.Username}' was rejected.", "Anonymous");
            MessageBox.Show(this, "Invalid username or password.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (admin.MustChangePassword)
        {
            using var changeForm = new ForcePasswordChangeForm(_authenticationService, admin);
            if (changeForm.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }
        }

        _auditService.Record(AuditCategories.Security, "Admin login", $"'{admin.Username}' signed in.", "Admin", admin.Id);
        _navigation.Navigate(() => new AdminDashboardForm(admin, _electionService, _databaseFactory, _navigation));
    }

    private void HandleVoterRegistration(object? sender, EventArgs e)
    {
        _navigation.Navigate(() => new VoterRegistrationForm(_databaseFactory, _navigation));
    }

    private void HandleVoterLogin(object? sender, EventArgs e)
    {
        using var loginForm = new LoginDialog("Voter Login", "Sign in with your voter account", isAdmin: false, useVoterNumber: true);
        UiTheme.ApplyDialogStyle(loginForm);

        if (loginForm.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var voter = _authenticationService.VoterLogin(loginForm.Username, loginForm.Password);
        if (voter == null)
        {
            _auditService.Record(AuditCategories.Security, "Voter login failed", $"Voter '{loginForm.Username}' was rejected.", "Anonymous");
            MessageBox.Show(this, "Invalid voter number/username or password.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _navigation.Navigate(() => new VoterDashboardForm(voter, _votingService, _databaseFactory, _navigation));
    }

    private void HandleViewCandidates(object? sender, EventArgs e)
    {
        _navigation.Navigate(() => new CandidateListForm(_databaseFactory, _navigation));
    }

    private void HandleElectionStatus(object? sender, EventArgs e)
    {
        _navigation.Navigate(() => new ElectionStatusForm(_databaseFactory, _navigation));
    }

    private void HandleReports(object? sender, EventArgs e)
    {
        _navigation.Navigate(() => new ReportsForm(_databaseFactory, _navigation));
    }

    private void HandleSettings(object? sender, EventArgs e)
    {
        _navigation.Navigate(() => new SettingsForm(_databaseFactory, _navigation));
    }
}

/// <summary>Large menu button with a glyph, title and supporting description.</summary>
internal sealed class MenuActionButton : Button
{
    private readonly string _glyph;
    private readonly string _description;
    private readonly Color _accent;
    private bool _hovered;

    internal MenuActionButton(string glyph, string text, string description, Color accent)
    {
        _glyph = glyph;
        _description = description;
        _accent = accent;
        Text = text;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
        BackColor = Color.White;
        Font = UiTheme.CreateFont(11f, FontStyle.Bold);
        Cursor = Cursors.Hand;
        TextAlign = ContentAlignment.MiddleLeft;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        MouseEnter += (_, _) => { _hovered = true; Invalidate(); };
        MouseLeave += (_, _) => { _hovered = false; Invalidate(); };
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        Color fill = _hovered ? ControlPaint.Light(_accent, 0.86f) : Color.White;
        using var path = RoundedPanel.CreateRoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), 12);
        using var background = new SolidBrush(fill);
        e.Graphics.FillPath(background, path);
        using var border = new Pen(_hovered ? _accent : UiTheme.Border);
        e.Graphics.DrawPath(border, path);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var iconRect = new Rectangle(16, (Height - 40) / 2, 40, 40);
        using (var iconPath = RoundedPanel.CreateRoundedPath(iconRect, 10))
        using (var iconBrush = new SolidBrush(_accent))
        {
            e.Graphics.FillPath(iconBrush, iconPath);
        }

        using var iconFont = new Font("Segoe MDL2 Assets", 15f);
        TextRenderer.DrawText(e.Graphics, _glyph, iconFont, iconRect, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

        TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(70, 8, Math.Max(0, Width - 86), 24), UiTheme.Ink, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        using var descFont = UiTheme.CreateFont(8.5f);
        TextRenderer.DrawText(e.Graphics, _description, descFont, new Rectangle(70, 28, Math.Max(0, Width - 86), 22), UiTheme.Muted, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);

        if (Focused && ShowFocusCues)
        {
            ControlPaint.DrawFocusRectangle(e.Graphics, new Rectangle(6, 6, Width - 12, Height - 12));
        }
    }
}
