using EVotingSystem.Models;
using EVotingSystem.Services;

namespace EVotingSystem.Forms;

/// <summary>
/// Forces the first-run administrator to replace the seeded default password
/// before the dashboard can be used.
/// </summary>
public class ForcePasswordChangeForm : Form
{
    private readonly AuthenticationService _authenticationService;
    private readonly Admin _admin;

    public ForcePasswordChangeForm(AuthenticationService authenticationService, Admin admin)
    {
        _authenticationService = authenticationService;
        _admin = admin;

        Text = "Change Password Required";
        Size = new Size(440, 300);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var noticeLabel = new Label
        {
            Text = "This account is using a default password.\nYou must set a new password before continuing.",
            Location = new Point(24, 18),
            Size = new Size(380, 40),
        };
        var currentLabel = new Label { Text = "Current password:", Location = new Point(24, 70), AutoSize = true };
        var currentText = new TextBox { Location = new Point(160, 66), Width = 240, UseSystemPasswordChar = true };
        var newLabel = new Label { Text = "New password:", Location = new Point(24, 112), AutoSize = true };
        var newText = new TextBox { Location = new Point(160, 108), Width = 240, UseSystemPasswordChar = true };
        var confirmLabel = new Label { Text = "Confirm password:", Location = new Point(24, 154), AutoSize = true };
        var confirmText = new TextBox { Location = new Point(160, 150), Width = 240, UseSystemPasswordChar = true };
        var saveButton = new Button { Text = "Save Password", Location = new Point(160, 196), Width = 240, Height = 36 };

        saveButton.Click += (_, _) =>
        {
            if (newText.Text != confirmText.Text)
            {
                MessageBox.Show(this, "The new passwords do not match.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = _authenticationService.ChangeAdminPassword(
                _admin.Id,
                currentText.Text,
                newText.Text);

            if (!result.Success)
            {
                MessageBox.Show(this, result.Message, "Password Not Changed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _admin.MustChangePassword = false;
            MessageBox.Show(this, result.Message, "Password Changed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        };

        Controls.AddRange(new Control[] { noticeLabel, currentLabel, currentText, newLabel, newText, confirmLabel, confirmText, saveButton });
        UiTheme.ApplyDialogStyle(this);
    }
}
