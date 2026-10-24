namespace EVotingSystem.Forms;

/// <summary>
/// Tracks whether a guarded form is allowed to close programmatically, so the
/// window's X button can be intercepted to navigate back instead of terminating the
/// application.
/// </summary>
internal sealed class ClosingGuard
{
    public bool AllowClose { get; set; }
}

/// <summary>
/// Central navigation for the whole application. Exactly one form is visible at a
/// time: opening a new screen hides (not closes) the current one and pushes it onto
/// a back-stack, so returning restores the correct parent in its previous state.
///
/// Lifecycle rules enforced here:
///  - <see cref="Navigate"/> hides the current form and shows the next one, reusing
///    an existing instance of the same type instead of opening duplicates.
///  - <see cref="Back"/> closes and disposes the current form and re-shows the parent.
///  - <see cref="Reset"/> (used by logout) tears everything down and returns to root.
///  - The application only exits via <see cref="ExitApplication"/>, never because an
///    intermediate form was closed.
/// </summary>
public sealed class FormNavigationService
{
    private readonly Stack<Form> _backStack = new();
    private readonly ApplicationContext _context;

    public FormNavigationService(ApplicationContext context)
    {
        _context = context;
    }

    public Form? Current { get; private set; }

    /// <summary>Shows the root (main menu) form as the application's main window.</summary>
    public void Start(Form root)
    {
        Current = root;
        _context.MainForm = root;
        root.FormClosed += (_, _) =>
        {
            if (ReferenceEquals(Current, root))
            {
                ExitApplication();
            }
        };
        root.Show();
    }

    /// <summary>
    /// Hides the current form, pushes it onto the back-stack and shows the next form.
    /// If the current form is already of the requested type it is reused.
    /// </summary>
    public T Navigate<T>(Func<T> factory) where T : Form
    {
        if (Current is T sameType && sameType.GetType() == typeof(T))
        {
            sameType.Activate();
            return sameType;
        }

        var next = factory();

        if (Current != null && !Current.IsDisposed)
        {
            _backStack.Push(Current);
            Current.Hide();
        }

        Current = next;
        _context.MainForm = next;
        GuardAgainstUserClose(next);
        next.Show();
        next.BringToFront();
        return next;
    }

    /// <summary>Returns to the previous form, closing and disposing the current one.</summary>
    public void Back()
    {
        var closing = Current;

        if (_backStack.Count == 0)
        {
            ExitApplication(closing);
            return;
        }

        var parent = _backStack.Pop();
        Current = parent;

        CloseAndDispose(closing);

        _context.MainForm = parent;
        parent.Show();
        parent.Activate();
        parent.BringToFront();
    }

    /// <summary>
    /// Logout: discards the entire navigation stack (clearing authenticated screens
    /// and temporary state) and returns to a fresh root menu form.
    /// </summary>
    public void Reset(Func<Form> rootFactory)
    {
        while (_backStack.Count > 0)
        {
            CloseAndDispose(_backStack.Pop());
        }

        CloseAndDispose(Current);

        var root = rootFactory();
        Current = root;
        _context.MainForm = root;
        root.FormClosed += (_, _) => ExitApplication();
        root.Show();
        root.Activate();
        root.BringToFront();
    }

    /// <summary>Closes every open form and ends the application cleanly.</summary>
    public void ExitApplication(Form? extra = null)
    {
        if (extra != null)
        {
            CloseAndDispose(extra);
        }

        while (_backStack.Count > 0)
        {
            CloseAndDispose(_backStack.Pop());
        }

        CloseAndDispose(Current);
        Current = null;

        _context.ExitThread();
    }

    private static void GuardAgainstUserClose(Form form)
    {
        var guard = new ClosingGuard();
        form.Tag = guard;
        form.FormClosing += (_, args) =>
        {
            if (args.CloseReason == CloseReason.UserClosing && !guard.AllowClose)
            {
                args.Cancel = true;
                // Route the X button through the same path as Back.
                form.BeginInvoke(new Action(() => (form as INavigationAware)?.RequestBack()));
            }
        };
    }

    private static void CloseAndDispose(Form? form)
    {
        if (form == null || form.IsDisposed)
        {
            return;
        }

        if (form.Tag is ClosingGuard guard)
        {
            guard.AllowClose = true;
        }

        form.Hide();
        form.Close();
        form.Dispose();
    }
}

/// <summary>
/// Implemented by forms that want their window X button to be routed back through
/// the navigation stack instead of closing arbitrarily.
/// </summary>
public interface INavigationAware
{
    void RequestBack();
}
