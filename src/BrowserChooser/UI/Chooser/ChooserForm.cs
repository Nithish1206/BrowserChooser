using BrowserChooser.Core;
using BrowserChooser.Core.Models;
using BrowserChooser.UI.Interop;

namespace BrowserChooser.UI.Chooser;

/// <summary>
/// The browser picker window, built to match the Windows 11 "Select an app to open this file" dialog: no title
/// bar, rounded corners, a list of choices and "Close" / "Open" buttons in a darker footer.
/// </summary>
internal sealed class ChooserForm : Form
{
    // Logical (96 DPI) layout, measured from the system dialog.
    private const int WindowWidth = 448;
    private const int SidePadding = 24;
    private const int TitleTop = 20;
    private const int TitleHeight = 28;
    private const int LinkHeight = 18;
    private const int FooterHeight = 60;
    private const int ButtonHeight = 32;
    private const int ButtonGap = 8;
    private const int MaxWindowHeight = 640;
    private const string Title = "Select a browser to open this link";

    private readonly ChooserTheme _theme;
    private readonly string _link;
    private readonly ChooserListControl _list;
    private readonly ChooserButton _closeButton;
    private readonly ChooserButton _openButton;
    private readonly ToolTip _toolTip = new();

    private ChooserFonts? _fonts;
    private Rectangle _linkBounds;
    private bool _linkIsTruncated;
    private bool _linkToolTipVisible;

    public ChooserForm(Uri url, IReadOnlyList<ChooserSection> sections, BrowserTarget? selected, ChooserTheme theme, BrowserIconProvider icons)
    {
        ArgumentNullException.ThrowIfNull(url);
        _theme = theme;
        _link = url.ToString();

        Text = AppIdentity.DisplayName;
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        StartPosition = FormStartPosition.Manual;
        // Every size is computed from DeviceDpi, so WinForms must not scale anything on top.
        AutoScaleMode = AutoScaleMode.None;
        KeyPreview = true;
        DoubleBuffered = true;
        BackColor = theme.Background;
        Icon = AppIcon.CreateDefault();

        _list = new ChooserListControl(theme, icons) { TabIndex = 0 };
        _list.SetSections(sections, selected);
        _list.EntryActivated += (_, _) => Choose();

        _closeButton = new ChooserButton(theme, theme.StandardButton, "Close") { TabIndex = 1 };
        _closeButton.Click += (_, _) => DialogResult = DialogResult.Cancel;

        // The primary action uses the Windows accent colour, like the system's own dialogs.
        _openButton = new ChooserButton(theme, theme.AccentButton, "Open") { TabIndex = 2 };
        _openButton.Click += (_, _) => Choose();

        Controls.Add(_list);
        Controls.Add(_closeButton);
        Controls.Add(_openButton);

        // Create the window on the monitor the user is working on, so it gets that monitor's DPI from the start.
        Rectangle area = Screen.FromPoint(Cursor.Position).WorkingArea;
        Location = new Point(area.Left + (area.Width / 3), area.Top + (area.Height / 4));
    }

    /// <summary>The user's choice, or <see langword="null"/> if the window was closed without one.</summary>
    public BrowserTarget? Result { get; private set; }

    /// <summary>
    /// A sizing frame is what makes Windows draw its standard window shadow. WM_NCCALCSIZE hides the frame and
    /// WM_NCHITTEST stops it from resizing, so only the shadow remains.
    /// </summary>
    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams parameters = base.CreateParams;
            parameters.Style |= NativeMethods.WS_THICKFRAME;
            return parameters;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        // Windows 11 rounded corners and a theme-matching border; ignored on Windows 10.
        NativeMethods.TrySetDwmAttribute(Handle, NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE, NativeMethods.DWMWCP_ROUND);
        NativeMethods.TrySetDwmAttribute(Handle, NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE, _theme.IsDark ? 1 : 0);
        NativeMethods.TrySetDwmAttribute(Handle, NativeMethods.DWMWA_BORDER_COLOR, ColorTranslator.ToWin32(_theme.WindowBorder));
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        ApplyLayout();
        CenterOn(Screen.FromPoint(Cursor.Position));
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Activate();
        _list.Focus();
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        ApplyLayout();
        CenterOn(Screen.FromControl(this));
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Escape:
                DialogResult = DialogResult.Cancel;
                return true;

            case Keys.Enter:
                if (ActiveControl is ChooserButton button)
                    button.PerformClick();
                else
                    Choose();
                return true;

            default:
                return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_fonts is null)
            return;

        Graphics graphics = e.Graphics;
        int padding = S(SidePadding);

        var titleBounds = new Rectangle(padding, S(TitleTop), ClientSize.Width - (padding * 2), S(TitleHeight));
        TextRenderer.DrawText(graphics, Title, _fonts.Title, titleBounds, _theme.TextPrimary, _theme.Background, Painting.SingleLine);
        TextRenderer.DrawText(graphics, _link, _fonts.Caption, _linkBounds, _theme.TextSecondary, _theme.Background, Painting.SingleLine);

        int footerTop = _list.Bottom;
        using (var footerBrush = new SolidBrush(_theme.FooterBackground))
            graphics.FillRectangle(footerBrush, 0, footerTop, ClientSize.Width, ClientSize.Height - footerTop);

        using var dividerPen = new Pen(_theme.FooterDivider);
        graphics.DrawLine(dividerPen, 0, footerTop, ClientSize.Width, footerTop);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        // A long link is cut short with "…"; show all of it on hover.
        bool overLink = _linkIsTruncated && _linkBounds.Contains(e.Location);
        if (overLink && !_linkToolTipVisible)
        {
            _toolTip.Show(_link, this, _linkBounds.Left, _linkBounds.Bottom + S(4));
            _linkToolTipVisible = true;
        }
        else if (!overLink && _linkToolTipVisible)
        {
            HideLinkToolTip();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        HideLinkToolTip();
    }

    /// <summary>
    /// Clicking anywhere outside the picker closes it, like a Windows flyout. Switching away with the keyboard
    /// (Alt+Tab, the Windows key) leaves it open, so a link isn't lost by accident.
    /// </summary>
    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        if (DialogResult == DialogResult.None && Visible && IsAnyMouseButtonDown())
            DialogResult = DialogResult.Cancel;
    }

    protected override void WndProc(ref Message m)
    {
        // The whole window is client area: no visible frame (see CreateParams).
        if (m.Msg == NativeMethods.WM_NCCALCSIZE && m.WParam != IntPtr.Zero)
        {
            m.Result = IntPtr.Zero;
            return;
        }

        // On focus changes (Alt+Tab, clicking another window) Windows repaints the frame, and with the frame
        // hidden it would draw a classic title bar over the content. -1 means "update the state, don't repaint".
        if (m.Msg == NativeMethods.WM_NCACTIVATE)
            m.LParam = new IntPtr(-1);

        base.WndProc(ref m);

        if (m.Msg == NativeMethods.WM_NCHITTEST)
        {
            // The hidden sizing frame must not resize the window.
            if (m.Result >= NativeMethods.HTLEFT && m.Result <= NativeMethods.HTBOTTOMRIGHT)
                m.Result = NativeMethods.HTCLIENT;

            // There is no title bar, so the empty area around the title acts as one for dragging.
            if (m.Result == NativeMethods.HTCLIENT)
            {
                long position = m.LParam.ToInt64();
                Point point = PointToClient(new Point((short)(position & 0xFFFF), (short)((position >> 16) & 0xFFFF)));
                if (point.Y < _list.Top && !_linkBounds.Contains(point))
                    m.Result = NativeMethods.HTCAPTION;
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _fonts?.Dispose();
            _toolTip.Dispose();
            Icon?.Dispose();
        }

        base.Dispose(disposing);
    }

    private int S(float logical) => Painting.Scale(logical, DeviceDpi);

    private void Choose()
    {
        if (_list.SelectedTarget is not { } target)
            return;

        // The browser starts after this window closes, and by then Windows has given focus back to the app the
        // link was clicked in, so the browser would open behind it. Hand our foreground right on now, while we
        // still have it, so the browser can bring itself to the front.
        NativeMethods.AllowSetForegroundWindow(NativeMethods.ASFW_ANY);

        Result = target;
        DialogResult = DialogResult.OK;
    }

    private void ApplyLayout()
    {
        int dpi = DeviceDpi;
        var fonts = new ChooserFonts(dpi);
        _list.ApplyScale(dpi, fonts);
        _closeButton.ApplyScale(dpi, fonts);
        _openButton.ApplyScale(dpi, fonts);
        _fonts?.Dispose();
        _fonts = fonts;

        int width = S(WindowWidth);
        int padding = S(SidePadding);
        int headerHeight = S(TitleTop + TitleHeight + LinkHeight);
        int footerHeight = S(FooterHeight);

        // Show every row when they fit; otherwise the list scrolls, as in the system dialog.
        int maxHeight = Math.Min(S(MaxWindowHeight), Screen.FromControl(this).WorkingArea.Height * 9 / 10);
        int listHeight = Math.Min(_list.ContentHeight, Math.Max(S(64), maxHeight - headerHeight - footerHeight));
        // Size, not ClientSize: ClientSize would add room for the hidden frame.
        Size = new Size(width, headerHeight + listHeight + footerHeight);

        _list.Bounds = new Rectangle(0, headerHeight, width, listHeight);

        // Two equal buttons, like the system dialog: Close on the left, Open on the right.
        int gap = S(ButtonGap);
        int buttonHeight = S(ButtonHeight);
        int buttonTop = headerHeight + listHeight + ((footerHeight - buttonHeight) / 2);
        int buttonWidth = (width - (padding * 2) - gap) / 2;
        _closeButton.Bounds = new Rectangle(padding, buttonTop, buttonWidth, buttonHeight);
        _openButton.Bounds = new Rectangle(padding + buttonWidth + gap, buttonTop, width - (padding * 2) - gap - buttonWidth, buttonHeight);

        _linkBounds = new Rectangle(padding, S(TitleTop + TitleHeight), width - (padding * 2), S(LinkHeight));
        _linkIsTruncated = TextRenderer.MeasureText(_link, fonts.Caption, Size.Empty, Painting.SingleLine).Width > _linkBounds.Width;

        Invalidate();
    }

    private void CenterOn(Screen screen)
    {
        Rectangle area = screen.WorkingArea;
        Location = new Point(area.Left + ((area.Width - Width) / 2), area.Top + ((area.Height - Height) / 2));
    }

    /// <summary>
    /// The physical button state: when another window is clicked, this window is deactivated while the button is
    /// still held down.
    /// </summary>
    private static bool IsAnyMouseButtonDown() =>
        NativeMethods.IsKeyDown(NativeMethods.VK_LBUTTON) ||
        NativeMethods.IsKeyDown(NativeMethods.VK_RBUTTON) ||
        NativeMethods.IsKeyDown(NativeMethods.VK_MBUTTON);

    private void HideLinkToolTip()
    {
        if (!_linkToolTipVisible)
            return;

        _toolTip.Hide(this);
        _linkToolTipVisible = false;
    }
}
