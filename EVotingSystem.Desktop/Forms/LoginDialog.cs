using EVotingSystem.Data;

namespace EVotingSystem.Forms;

/// <summary>
/// Reusable credential prompt for admin and voter sign-in. Authentication is
/// performed by the caller; this dialog only collects input and validates that
/// both fields are present. Passwords are never prefilled.
/// </summary>
public class LoginDialog : Form
{
    private readonly TextBox _usernameText = new();
    private readonly TextBox _passwordText = new();
    private readonly Label _hintLabel = new();

    public string Username => _usernameText.Text.Trim();
    public string Password => _passwordText.Text.Trim();

    public LoginDialog(string title, string subtitle, bool isAdmin, bool useVoterNumber = false)
    {
        Text = title;
        Size = new Size(480, 560);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UiTheme.HeaderStart;

        var banner = new GradientPanel
        {
            Dock = DockStyle.Top,
            Height = 150,
            StartColor = UiTheme.HeaderStart,
            EndColor = UiTheme.HeaderEnd,
        };

        var shield = new Label
        {
            Text = isAdmin ? "\uEA18" : "\uE77B",
            Font = new Font("Segoe MDL2 Assets", 34f),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Dock = DockStyle.Top,
            Height = 84,
            TextAlign = ContentAlignment.MiddleCenter,
        };

        var heading = new Label
        {
            Text = title,
            Font = UiTheme.CreateFont(17f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Dock = DockStyle.Top,
            Height = 30,
            TextAlign = ContentAlignment.MiddleCenter,
        };

        var sub = new Label
        {
            Text = subtitle,
            Font = UiTheme.CreateFont(9f),
            ForeColor = UiTheme.HeaderSubtext,
            BackColor = Color.Transparent,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.TopCenter,
        };
        banner.Controls.Add(sub);
        banner.Controls.Add(heading);
        banner.Controls.Add(shield);

        var card = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(24),
            Padding = new Padding(28, 22, 28, 22),
            BackColor = UiTheme.Surface,
        };

        var usernameLabel = new Label
        {
            Text = useVoterNumber ? "Voter number / Username" : "Username",
            Dock = DockStyle.Top,
            Height = 22,
            Font = UiTheme.CreateFont(9f, FontStyle.Bold),
            ForeColor = UiTheme.Ink,
        };
        _usernameText.Dock = DockStyle.Top;
        _usernameText.Font = UiTheme.CreateFont(11f);
        _usernameText.BorderStyle = BorderStyle.FixedSingle;

        var spacer = new Panel { Dock = DockStyle.Top, Height = 14, BackColor = UiTheme.Surface };

        var passwordLabel = new Label
        {
            Text = "Password",
            Dock = DockStyle.Top,
            Height = 22,
            Font = UiTheme.CreateFont(9f, FontStyle.Bold),
            ForeColor = UiTheme.Ink,
        };
        var passwordRow = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = UiTheme.Surface };
        _passwordText.Location = new Point(0, 2);
        _passwordText.Width = 300;
        _passwordText.Font = UiTheme.CreateFont(11f);
        _passwordText.UseSystemPasswordChar = true;
        _passwordText.BorderStyle = BorderStyle.FixedSingle;
        var toggle = UiTheme.CreatePasswordVisibilityToggle(_passwordText);
        passwordRow.Controls.Add(_passwordText);
        passwordRow.Controls.Add(toggle);

        var spacer2 = new Panel { Dock = DockStyle.Top, Height = 10, BackColor = UiTheme.Surface };

        _hintLabel.Dock = DockStyle.Top;
        _hintLabel.Height = 34;
        _hintLabel.Font = UiTheme.CreateFont(8.5f);
        _hintLabel.ForeColor = UiTheme.Muted;

        var buttonRow = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = UiTheme.Surface };
        var loginButton = new IconButton("\uE8FB", "Login")
        {
            Dock = DockStyle.Left,
            Width = 210,
            BackColor = UiTheme.Green,
            HoverColor = ControlPaint.Light(UiTheme.Green, 0.12f),
        };
        var cancelButton = new IconButton("\uE72B", "Back to Menu")
        {
            Dock = DockStyle.Right,
            Width = 150,
            BackColor = UiTheme.Coral,
            HoverColor = ControlPaint.Light(UiTheme.Coral, 0.12f),
        };

        buttonRow.Controls.Add(cancelButton);
        buttonRow.Controls.Add(loginButton);

        AcceptButton = loginButton;
        CancelButton = cancelButton;

        cancelButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        loginButton.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_usernameText.Text) || string.IsNullOrWhiteSpace(_passwordText.Text))
            {
                MessageBox.Show(this, "Enter both fields before continuing.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        };

        card.Controls.Add(buttonRow);
        card.Controls.Add(_hintLabel);
        card.Controls.Add(spacer2);
        card.Controls.Add(passwordRow);
        card.Controls.Add(passwordLabel);
        card.Controls.Add(spacer);
        card.Controls.Add(_usernameText);
        card.Controls.Add(usernameLabel);

        var cardHost = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.HeaderStart, Padding = new Padding(24, 8, 24, 24) };
        cardHost.Controls.Add(card);

        Controls.Add(cardHost);
        Controls.Add(banner);
    }

    public void SetExtraHint(string hint)
    {
        _hintLabel.Text = hint;
    }
}
