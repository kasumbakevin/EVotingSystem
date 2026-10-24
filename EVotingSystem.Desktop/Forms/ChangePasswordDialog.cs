using EVotingSystem.Data;
using EVotingSystem.Services;

namespace EVotingSystem.Forms;

/// <summary>
/// Lets an administrator rotate their own password. Stored passwords are never
/// displayed; the current password must be supplied to authorise the change.
/// </summary>
public class ChangePasswordDialog : Form
{
    private readonly int _adminId;
    private readonly AuthenticationService _authenticationService;

    public ChangePasswordDialog(int adminId, DatabaseFactory databaseFactory)
    {
        _adminId = adminId;
        _authenticationService = new AuthenticationService(databaseFactory);

        Text = "Change Password";
        Size = new Size(440, 300);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var currentLabel = new Label { Text = "Current password:", Location = new Point(24, 34), AutoSize = true };
        var currentText = new TextBox { Location = new Point(180, 30), Width = 220, UseSystemPasswordChar = true };
        var newLabel = new Label { Text = "New password:", Location = new Point(24, 88), AutoSize = true };
        var newText = new TextBox { Location = new Point(180, 84), Width = 220, UseSystemPasswordChar = true };
        var confirmLabel = new Label { Text = "Confirm password:", Location = new Point(24, 142), AutoSize = true };
        var confirmText = new TextBox { Location = new Point(180, 138), Width = 220, UseSystemPasswordChar = true };
        var saveButton = new Button { Text = "Change Password", Location = new Point(180, 186), Width = 220, Height = 36 };

        saveButton.Click += (_, _) =>
        {
            if (newText.Text != confirmText.Text)
            {
                MessageBox.Show(this, "The new passwords do not match.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = _authenticationService.ChangeAdminPassword(_adminId, currentText.Text, newText.Text);

            if (!result.Success)
            {
                MessageBox.Show(this, result.Message, "Password Not Changed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show(this, result.Message, "Password Changed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        };

        Controls.AddRange(new Control[] { currentLabel, currentText, newLabel, newText, confirmLabel, confirmText, saveButton });
        UiTheme.ApplyDialogStyle(this);
    }
}
