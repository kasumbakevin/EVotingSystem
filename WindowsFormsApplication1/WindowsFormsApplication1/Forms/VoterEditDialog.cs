using EVotingSystem.Models;
using EVotingSystem.Services;

namespace EVotingSystem.Forms;

/// <summary>
/// Add or edit a single voter record. Registration captures demographics and
/// contact details; verification is performed separately from the register.
/// </summary>
public class VoterEditDialog : Form
{
    private readonly VoterService _voterService;
    private readonly Voter? _existing;
    private readonly PhotoStore _photoStore;
    private readonly CameraCapture _camera = new();
    private byte[]? _pickedPhoto;

    public VoterEditDialog(VoterService voterService, Voter? existing)
    {
        _voterService = voterService;
        _existing = existing;
        _photoStore = new PhotoStore();

        Text = existing == null ? "Register Voter" : "Edit Voter";
        Size = new Size(480, 470);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        FormClosed += (_, _) => _camera.Dispose();

        var nameLabel = new Label { Text = "Full name:", Location = new Point(24, 24), AutoSize = true };
        var nameText = new TextBox { Location = new Point(160, 20), Width = 280, Text = existing?.Name ?? string.Empty };

        var usernameLabel = new Label { Text = "Username:", Location = new Point(24, 62), AutoSize = true };
        var usernameText = new TextBox { Location = new Point(160, 58), Width = 280, Text = existing?.Username ?? string.Empty };

        var voterNumberLabel = new Label { Text = "Voter number:", Location = new Point(24, 100), AutoSize = true };
        var voterNumberText = new TextBox { Location = new Point(160, 96), Width = 280, Text = existing?.VoterNumber ?? string.Empty };

        var departmentLabel = new Label { Text = "Department:", Location = new Point(24, 138), AutoSize = true };
        var departmentText = new TextBox { Location = new Point(160, 134), Width = 280, Text = existing?.Department ?? string.Empty };

        var emailLabel = new Label { Text = "Email:", Location = new Point(24, 176), AutoSize = true };
        var emailText = new TextBox { Location = new Point(160, 172), Width = 280, Text = existing?.Email ?? string.Empty };

        var phoneLabel = new Label { Text = "Phone:", Location = new Point(24, 214), AutoSize = true };
        var phoneText = new TextBox { Location = new Point(160, 210), Width = 280, Text = existing?.PhoneNumber ?? string.Empty };

        var passwordLabel = new Label { Text = "Password:", Location = new Point(24, 252), AutoSize = true };
        var passwordText = new TextBox { Location = new Point(160, 248), Width = 240, UseSystemPasswordChar = true };
        var passwordToggle = UiTheme.CreatePasswordVisibilityToggle(passwordText);

        if (existing != null)
        {
            passwordLabel.Visible = false;
            passwordText.Visible = false;
            passwordToggle.Visible = false;
        }

        var photoLabel = new Label { Text = "Reference photo:", Location = new Point(24, 288), AutoSize = true };
        var captureButton = new Button
        {
            Text = _camera.IsLiveCameraAvailable ? "Capture Photo" : "Select Photo",
            Location = new Point(160, 284),
            Width = 140,
            Height = 30,
        };
        var photoStatus = new Label
        {
            Text = string.IsNullOrWhiteSpace(existing?.PhotoPath) ? "Not set" : "On file",
            Location = new Point(310, 290),
            AutoSize = true,
            ForeColor = UiTheme.Muted,
        };
        captureButton.Click += (_, _) =>
        {
            var bytes = _camera.Capture();
            if (bytes == null)
            {
                return;
            }

            _pickedPhoto = bytes;
            photoStatus.Text = "Captured (unsaved)";
            photoStatus.ForeColor = UiTheme.Green;
        };

        var saveButton = new Button
        {
            Text = existing == null ? "Register" : "Save Changes",
            Location = new Point(160, 330),
            Width = 280,
            Height = 38,
        };

        saveButton.Click += (_, _) =>
        {
            ServiceResult result;
            if (_existing == null)
            {
                result = _voterService.Register(
                    nameText.Text.Trim(),
                    usernameText.Text.Trim(),
                    passwordText.Text,
                    voterNumberText.Text.Trim(),
                    NullIfEmpty(departmentText.Text),
                    NullIfEmpty(emailText.Text),
                    NullIfEmpty(phoneText.Text));
            }
            else
            {
                result = _voterService.Update(
                    _existing.Id,
                    nameText.Text.Trim(),
                    usernameText.Text.Trim(),
                    voterNumberText.Text.Trim(),
                    NullIfEmpty(departmentText.Text),
                    NullIfEmpty(emailText.Text),
                    NullIfEmpty(phoneText.Text));
            }

            MessageBox.Show(
                this,
                result.Message,
                result.Success ? "Success" : "Not Saved",
                MessageBoxButtons.OK,
                result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

            if (result.Success)
            {
                SavePhotoIfPicked(nameText.Text.Trim(), usernameText.Text.Trim());
                DialogResult = DialogResult.OK;
                Close();
            }
        };

        Controls.AddRange(new Control[]
        {
            nameLabel, nameText,
            usernameLabel, usernameText,
            voterNumberLabel, voterNumberText,
            departmentLabel, departmentText,
            emailLabel, emailText,
            phoneLabel, phoneText,
            passwordLabel, passwordText, passwordToggle,
            photoLabel, captureButton, photoStatus,
            saveButton,
        });
        UiTheme.ApplyDialogStyle(this);
    }

    private void SavePhotoIfPicked(string name, string username)
    {
        if (_pickedPhoto == null)
        {
            return;
        }

        var voterId = _existing?.Id;
        if (voterId == null)
        {
            var created = _voterService.GetAll()
                .FirstOrDefault(v => string.Equals(v.Username, username, StringComparison.OrdinalIgnoreCase));
            voterId = created?.Id;
        }

        if (voterId == null)
        {
            return;
        }

        var path = _photoStore.SaveReference(voterId.Value, _pickedPhoto);
        _voterService.SetReferencePhoto(voterId.Value, path);
    }

    private static string? NullIfEmpty(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
