using System.Drawing.Drawing2D;
using EVotingSystem.Models;

namespace EVotingSystem.Forms;

internal static class UiTheme
{
    internal static readonly Color Canvas = Color.FromArgb(246, 249, 252);
    internal static readonly Color Surface = Color.White;
    internal static readonly Color Ink = Color.FromArgb(30, 41, 59);
    internal static readonly Color Muted = Color.FromArgb(100, 116, 139);
    internal static readonly Color Green = Color.FromArgb(37, 99, 235);
    internal static readonly Color Mint = Color.FromArgb(239, 246, 255);
    internal static readonly Color Border = Color.FromArgb(226, 232, 240);
    internal static readonly Color HeaderStart = Color.FromArgb(37, 99, 235);
    internal static readonly Color HeaderEnd = Color.FromArgb(59, 130, 246);
    internal static readonly Color HeaderSubtext = Color.FromArgb(219, 234, 254);
    internal static readonly Color Teal = Color.FromArgb(14, 165, 233);
    internal static readonly Color Ocean = Color.FromArgb(59, 130, 246);
    internal static readonly Color Slate = Color.FromArgb(71, 85, 105);
    internal static readonly Color Success = Color.FromArgb(16, 185, 129);
    internal static readonly Color Amber = Color.FromArgb(245, 158, 11);
    internal static readonly Color Coral = Color.FromArgb(239, 68, 68);
    internal static readonly Color RowAlternate = Color.FromArgb(247, 250, 250);

    internal static Font DefaultFont => CreateFont(10f);

    internal static Font CreateFont(float size, FontStyle style = FontStyle.Regular, string familyName = "Segoe UI")
    {
        try
        {
            return new Font(familyName, size, style);
        }
        catch (Exception)
        {
            return new Font(FontFamily.GenericSansSerif, size, style);
        }
    }

    internal static void ApplyGlobalFont(Control parent)
    {
        parent.Font ??= DefaultFont;

        foreach (Control child in parent.Controls)
        {
            ApplyGlobalFont(child);
        }
    }

    internal static CheckBox CreatePasswordVisibilityToggle(TextBox passwordText)
    {
        var toolTip = new ToolTip();
        var toggle = new CheckBox
        {
            Appearance = Appearance.Button,
            AutoSize = false,
            Location = new Point(passwordText.Right + 6, passwordText.Top - 2),
            Size = new Size(30, 28),
            Font = new Font("Segoe MDL2 Assets", 12),
            Text = "\uE890",
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Muted,
            BackColor = Surface,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            AccessibleName = "Show password",
            UseVisualStyleBackColor = false,
        };
        toggle.FlatAppearance.BorderSize = 0;
        toggle.FlatAppearance.CheckedBackColor = Mint;
        toggle.CheckedChanged += (_, _) =>
        {
            passwordText.UseSystemPasswordChar = !toggle.Checked;
            toggle.Text = toggle.Checked ? "\uE7B3" : "\uE890";
            toggle.AccessibleName = toggle.Checked ? "Hide password" : "Show password";
            toolTip.SetToolTip(toggle, toggle.AccessibleName);
        };
        toggle.Disposed += (_, _) => toolTip.Dispose();
        toolTip.SetToolTip(toggle, "Show password");
        return toggle;
    }

    internal static void StyleGrid(DataGridView grid)
    {
        grid.BackgroundColor = Surface;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.GridColor = Border;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.ColumnHeadersHeight = 38;
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Ink,
            ForeColor = Color.White,
            Font = CreateFont(9.5f, FontStyle.Bold),
            SelectionBackColor = Ink,
            SelectionForeColor = Color.White,
            Padding = new Padding(8, 0, 0, 0),
        };
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Surface,
            ForeColor = Ink,
            Font = CreateFont(9.5f),
            SelectionBackColor = Mint,
            SelectionForeColor = Ink,
            Padding = new Padding(8, 4, 8, 4),
        };
        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = RowAlternate,
            ForeColor = Ink,
            SelectionBackColor = Mint,
            SelectionForeColor = Ink,
        };
    }

    internal static Color StatusColor(ElectionStatus status) => status switch
    {
        ElectionStatus.Open => Success,
        ElectionStatus.Paused => Amber,
        ElectionStatus.Closed => Slate,
        ElectionStatus.Cancelled => Coral,
        ElectionStatus.Archived => Muted,
        ElectionStatus.Scheduled => Ocean,
        ElectionStatus.PendingApproval => Amber,
        _ => Muted,
    };

    internal static Color VerificationColor(VoterVerificationStatus status) => status switch
    {
        VoterVerificationStatus.Verified => Success,
        VoterVerificationStatus.ManualReview => Amber,
        VoterVerificationStatus.Rejected => Coral,
        _ => Muted,
    };

    /// <summary>Creates a small rounded status pill (e.g. OPEN, PAUSED, VERIFIED).</summary>
    internal static RoundedPanel CreateStatusBadge(string text, Color color)
    {
        var badge = new RoundedPanel
        {
            CornerRadius = 10,
            BorderColor = color,
            BackColor = ControlPaint.Light(color, 0.86f),
            AutoSize = false,
            Height = 26,
            Width = 96,
        };
        var label = new Label
        {
            Text = text.ToUpperInvariant(),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = color,
            BackColor = Color.Transparent,
            Font = CreateFont(8.5f, FontStyle.Bold),
        };
        badge.Controls.Add(label);
        badge.Tag = label;
        return badge;
    }

    internal static void SetStatusBadge(RoundedPanel badge, string text, Color color)
    {
        badge.BorderColor = color;
        badge.BackColor = ControlPaint.Light(color, 0.86f);
        badge.Invalidate();
        if (badge.Tag is Label label)
        {
            label.Text = text.ToUpperInvariant();
            label.ForeColor = color;
        }
    }

    internal static void ApplyDialogStyle(Form form)
    {
        form.BackColor = Canvas;
        form.Font = DefaultFont;
        foreach (Control control in form.Controls)
        {
            if (control is Label label)
            {
                label.ForeColor = Ink;
                label.Font = CreateFont(9f);
            }
            else if (control is TextBox textBox)
            {
                textBox.Font = CreateFont(10f);
                textBox.BorderStyle = BorderStyle.FixedSingle;
            }
            else if (control is Button button)
            {
                button.BackColor = Green;
                button.ForeColor = Color.White;
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderSize = 0;
                button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(Green, 0.08f);
                button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(Green, 0.08f);
                button.Font = CreateFont(9.5f, FontStyle.Bold);
                button.Cursor = Cursors.Hand;
                button.UseVisualStyleBackColor = false;
                button.TextAlign = ContentAlignment.MiddleCenter;
                button.Height = Math.Max(button.Height, 38);
            }
        }

        var surface = new RoundedPanel
        {
            Location = new Point(8, 8),
            Size = new Size(Math.Max(1, form.ClientSize.Width - 16), Math.Max(1, form.ClientSize.Height - 16)),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.White,
        };
        form.Controls.Add(surface);
        surface.SendToBack();
    }
}

internal sealed class GradientPanel : Panel
{
    internal Color StartColor = UiTheme.HeaderStart;
    internal Color EndColor = UiTheme.HeaderEnd;

    internal GradientPanel()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using var brush = new LinearGradientBrush(ClientRectangle, StartColor, EndColor, 0f);
        e.Graphics.FillRectangle(brush, ClientRectangle);
    }
}

internal class RoundedPanel : Panel
{
    internal int CornerRadius = 16;
    internal Color BorderColor = UiTheme.Border;

    internal RoundedPanel()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.ResizeRedraw, true);
        BackColor = Color.White;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = ClientRectangle;
        bounds.Width--;
        bounds.Height--;
        using var path = CreateRoundedPath(bounds, CornerRadius);
        using var pen = new Pen(BorderColor);
        e.Graphics.DrawPath(pen, path);
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        if (Width < 2 || Height < 2)
        {
            return;
        }

        using var path = CreateRoundedPath(new Rectangle(0, 0, Width, Height), CornerRadius);
        Region = new Region(path);
    }

    internal static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
    {
        int diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class IconButton : Button
{
    internal string Glyph;
    internal Color HoverColor = UiTheme.Teal;
    private bool _hovered;

    internal IconButton(string glyph, string text)
    {
        Glyph = glyph;
        Text = text;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
        BackColor = UiTheme.Green;
        ForeColor = Color.White;
        Font = UiTheme.CreateFont(10f, FontStyle.Bold);
        Cursor = Cursors.Hand;
        Height = 44;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        MouseEnter += (_, _) => { _hovered = true; Invalidate(); };
        MouseLeave += (_, _) => { _hovered = false; Invalidate(); };
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using var background = new SolidBrush(Parent?.BackColor ?? UiTheme.Canvas);
        e.Graphics.FillRectangle(background, ClientRectangle);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new Rectangle(1, 1, Math.Max(1, Width - 2), Math.Max(1, Height - 2));
        using var path = RoundedPanel.CreateRoundedPath(bounds, 10);
        using var background = new SolidBrush(_hovered ? HoverColor : BackColor);
        e.Graphics.FillPath(background, path);

        using var iconFont = new Font("Segoe MDL2 Assets", 14, FontStyle.Regular);
        TextRenderer.DrawText(e.Graphics, Glyph, iconFont, new Rectangle(16, 0, 28, Height), ForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(48, 0, Math.Max(0, Width - 60), Height), ForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);

        if (Focused && ShowFocusCues)
        {
            ControlPaint.DrawFocusRectangle(e.Graphics, new Rectangle(5, 5, Width - 10, Height - 10));
        }
    }
}

internal sealed class VoteBarChart : Panel
{
    internal const int RowHeight = 64;

    internal IReadOnlyList<Candidate> Candidates = Array.Empty<Candidate>();
    internal int TotalVotes;

    internal static int PreferredHeight(int candidateCount)
        => Math.Max(80, candidateCount * RowHeight + 16);

    internal VoteBarChart()
    {
        DoubleBuffered = true;
        BackColor = Color.White;
        SetStyle(ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var candidates = Candidates.OrderByDescending(candidate => candidate.VoteCount).ToList();
        if (candidates.Count == 0)
        {
            TextRenderer.DrawText(e.Graphics, "No candidates added yet", Font, ClientRectangle, UiTheme.Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        const int rowHeight = RowHeight;
        const int detailsWidth = 250;
        const int resultWidth = 150;
        int chartWidth = Math.Max(40, Width - detailsWidth - resultWidth - 16);
        using var nameFont = UiTheme.CreateFont(9f, FontStyle.Bold);
        using var partyFont = UiTheme.CreateFont(8f);
        using var resultFont = UiTheme.CreateFont(9f);
        for (int index = 0; index < candidates.Count; index++)
        {
            var candidate = candidates[index];
            int y = 8 + index * rowHeight;
            TextRenderer.DrawText(e.Graphics, candidate.Name, nameFont, new Rectangle(4, y, detailsWidth, 22), UiTheme.Ink, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(e.Graphics, $"Party: {candidate.Party}", partyFont, new Rectangle(4, y + 22, detailsWidth, 20), UiTheme.Muted, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);

            var track = new Rectangle(detailsWidth + 10, y + 42, chartWidth, 10);
            using var trackBrush = new SolidBrush(UiTheme.Canvas);
            e.Graphics.FillRectangle(trackBrush, track);

            float ratio = TotalVotes == 0 ? 0 : (float)candidate.VoteCount / TotalVotes;
            int fillWidth = (int)(track.Width * ratio);
            if (fillWidth > 0)
            {
                using var barBrush = new SolidBrush(index == 0 ? UiTheme.Green : UiTheme.Ocean);
                e.Graphics.FillRectangle(barBrush, new Rectangle(track.X, track.Y, fillWidth, track.Height));
            }

            string result = $"{candidate.VoteCount:N0} votes  |  {ratio:P0}";
            TextRenderer.DrawText(e.Graphics, result, resultFont, new Rectangle(Width - resultWidth - 4, y + 36, resultWidth, 22), UiTheme.Muted, TextFormatFlags.VerticalCenter | TextFormatFlags.Right | TextFormatFlags.EndEllipsis);
        }
    }
}

/// <summary>
/// A dark navigation rail used on management screens (Dashboard, Voters,
/// Candidates, ...). Each entry raises <see cref="ItemSelected"/> with its key.
/// </summary>
internal sealed class SidebarNav : Panel
{
    internal event Action<string>? ItemSelected;

    private readonly FlowLayoutPanel _items = new();
    private readonly Dictionary<string, NavItemButton> _buttons = new();
    internal Label HeaderLabel = new();

    internal SidebarNav(string header)
    {
        Dock = DockStyle.Left;
        Width = 232;
        BackColor = UiTheme.HeaderStart;
        Padding = new Padding(0);

        HeaderLabel = new Label
        {
            Text = header.ToUpperInvariant(),
            Dock = DockStyle.Top,
            Height = 68,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(20, 0, 0, 0),
            Font = UiTheme.CreateFont(12f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
        };

        _items.Dock = DockStyle.Fill;
        _items.FlowDirection = FlowDirection.TopDown;
        _items.WrapContents = false;
        _items.BackColor = UiTheme.HeaderStart;
        _items.Padding = new Padding(10, 8, 10, 8);
        _items.AutoScroll = true;
        _items.SizeChanged += (_, _) => ResizeItems();

        Controls.Add(_items);
        Controls.Add(HeaderLabel);
    }

    internal void AddItem(string key, string glyph, string text)
    {
        var button = new NavItemButton(glyph, text, UiTheme.HeaderStart)
        {
            Width = Math.Max(160, _items.ClientSize.Width - _items.Padding.Horizontal - 4),
            Height = 44,
            Margin = new Padding(0, 2, 0, 2),
            Tag = key,
        };
        button.Click += (_, _) => Select(key);
        _buttons[key] = button;
        _items.Controls.Add(button);
    }

    internal void Select(string key)
    {
        foreach (var pair in _buttons)
        {
            pair.Value.SetActive(pair.Key == key);
        }

        ItemSelected?.Invoke(key);
    }

    private void ResizeItems()
    {
        int width = Math.Max(160, _items.ClientSize.Width - _items.Padding.Horizontal - 4);
        foreach (Control control in _items.Controls)
        {
            control.Width = width;
        }
    }
}

/// <summary>Flat navigation button that shows an active accent bar when selected.</summary>
internal sealed class NavItemButton : Button
{
    internal void SetActive(bool active)
    {
        _active = active;
        Invalidate();
    }
    private readonly Color _surfaceColor;
    private bool _active;
    private bool _hovered;

    internal NavItemButton(string glyph, string text, Color surfaceColor) : base()
    {
        Text = text;
        _surfaceColor = surfaceColor;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
        BackColor = surfaceColor;
        ForeColor = UiTheme.HeaderSubtext;
        Font = UiTheme.CreateFont(9.5f, FontStyle.Bold);
        Cursor = Cursors.Hand;
        TextAlign = ContentAlignment.MiddleLeft;
        Padding = new Padding(40, 0, 0, 0);
        GlyphText = glyph;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        MouseEnter += (_, _) => { _hovered = true; Invalidate(); };
        MouseLeave += (_, _) => { _hovered = false; Invalidate(); };
    }

    internal string GlyphText { get; }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        Color color = _active
            ? UiTheme.Teal
            : _hovered ? ControlPaint.Light(_surfaceColor, 0.10f) : _surfaceColor;
        using var brush = new SolidBrush(color);
        e.Graphics.FillRectangle(brush, ClientRectangle);

        if (_active)
        {
            using var accent = new SolidBrush(Color.White);
            e.Graphics.FillRectangle(accent, new Rectangle(0, 6, 4, Height - 12));
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var iconFont = new Font("Segoe MDL2 Assets", 13f);
        Color fore = _active ? Color.White : UiTheme.HeaderSubtext;
        TextRenderer.DrawText(e.Graphics, GlyphText, iconFont, new Rectangle(16, 0, 24, Height), fore, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(44, 0, Math.Max(0, Width - 52), Height), fore, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
    }
}

/// <summary>White rounded card with an optional title and a content area.</summary>
internal sealed class SectionCard : RoundedPanel
{
    internal Panel Body { get; }
    internal Label TitleLabel { get; }

    internal SectionCard(string title, int bodyTop = 44)
    {
        Dock = DockStyle.Fill;
        BackColor = UiTheme.Surface;
        CornerRadius = 14;
        Padding = new Padding(16, 12, 16, 12);

        TitleLabel = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = bodyTop,
            Font = UiTheme.CreateFont(11f, FontStyle.Bold),
            ForeColor = UiTheme.Ink,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        Body = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.Surface,
        };

        Controls.Add(Body);
        Controls.Add(TitleLabel);
    }
}

/// <summary>Stat card with icon, caption, big value and a footnote.</summary>
internal sealed class StatCard : RoundedPanel
{
    internal Label ValueLabel { get; }
    internal Label FootNoteLabel { get; }

    internal StatCard(string glyph, string caption, Color accent)
    {
        Width = 240;
        Height = 104;
        Margin = new Padding(0, 0, 12, 0);
        BackColor = UiTheme.Surface;
        CornerRadius = 14;

        var icon = new Label
        {
            Text = glyph,
            Font = new Font("Segoe MDL2 Assets", 16f),
            ForeColor = Color.White,
            BackColor = accent,
            Location = new Point(14, 16),
            Size = new Size(40, 40),
            TextAlign = ContentAlignment.MiddleCenter,
        };

        var captionLabel = new Label
        {
            Text = caption.ToUpperInvariant(),
            ForeColor = UiTheme.Muted,
            Font = UiTheme.CreateFont(8.5f, FontStyle.Bold),
            Location = new Point(66, 18),
            AutoSize = true,
        };

        ValueLabel = new Label
        {
            Text = "0",
            ForeColor = UiTheme.Ink,
            Font = UiTheme.CreateFont(22f, FontStyle.Bold),
            Location = new Point(64, 38),
            AutoSize = true,
        };

        FootNoteLabel = new Label
        {
            Text = string.Empty,
            ForeColor = UiTheme.Muted,
            Font = UiTheme.CreateFont(8.5f),
            Location = new Point(66, 72),
            AutoSize = true,
        };

        Controls.Add(icon);
        Controls.Add(captionLabel);
        Controls.Add(ValueLabel);
        Controls.Add(FootNoteLabel);
    }
}