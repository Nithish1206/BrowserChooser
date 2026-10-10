namespace BrowserChooser.UI.Chooser;

/// <summary>
/// A Windows 11 style button: rounded, with a subtle elevation border and hover/pressed fills. The colours decide
/// whether it is a standard or an accent (primary action) button.
/// </summary>
internal sealed class ChooserButton : Control
{
    private const int Radius = 4;

    private readonly ChooserTheme _theme;
    private readonly ButtonColors _colors;
    private ChooserFonts? _fonts;
    private int _dpi = 96;
    private bool _hovered;
    private bool _pressed;

    public ChooserButton(ChooserTheme theme, ButtonColors colors, string text)
    {
        _theme = theme;
        _colors = colors;
        Text = text;
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.StandardClick,
            true);
        SetStyle(ControlStyles.StandardDoubleClick, false);
        TabStop = true;
        BackColor = theme.FooterBackground;
        AccessibleRole = AccessibleRole.PushButton;
    }

    public void ApplyScale(int dpi, ChooserFonts fonts)
    {
        _dpi = dpi;
        _fonts = fonts;
        Invalidate();
    }

    public void PerformClick()
    {
        if (Enabled)
            OnClick(EventArgs.Empty);
    }

    protected override AccessibleObject CreateAccessibilityInstance() => new ButtonAccessibleObject(this);

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics graphics = e.Graphics;
        graphics.Clear(_theme.FooterBackground);
        Painting.PrepareForShapes(graphics);

        Color fill = _pressed ? _colors.FillPressed : _hovered ? _colors.FillHover : _colors.Fill;
        float radius = Painting.Scale(Radius, _dpi);
        var outer = new RectangleF(0, 0, Width, Height);

        // Borders are drawn as filled shapes so they stay one crisp pixel: the bottom edge shows through as the
        // slightly different "elevation" colour.
        Painting.FillRoundedRectangle(graphics, _colors.BorderBottom, outer, radius);
        Painting.FillRoundedRectangle(graphics, _colors.BorderTop, outer with { Height = Height - 1 }, radius);
        Painting.FillRoundedRectangle(graphics, fill, RectangleF.Inflate(outer, -1, -1), radius - 1);

        if (Focused && ShowFocusCues)
        {
            float stroke = Painting.Scale(2, _dpi);
            Painting.DrawRoundedRectangle(graphics, _theme.FocusStroke, stroke, RectangleF.Inflate(outer, -stroke / 2, -stroke / 2), radius);
        }

        if (_fonts is not null)
        {
            TextRenderer.DrawText(
                graphics, Text, _fonts.Body, ClientRectangle, _colors.Text, fill,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
        }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _hovered = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hovered = false;
        _pressed = false;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left)
        {
            Focus();
            _pressed = true;
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _pressed = false;
        Invalidate();
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.KeyCode == Keys.Space)
            PerformClick();
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate();
    }

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        AccessibleName = Text;
        Invalidate();
    }

    /// <summary>Lets screen readers and other assistive tools press the button.</summary>
    private sealed class ButtonAccessibleObject(ChooserButton owner) : ControlAccessibleObject(owner)
    {
        public override AccessibleRole Role => AccessibleRole.PushButton;

        public override string DefaultAction => "Press";

        public override void DoDefaultAction() => owner.PerformClick();
    }
}
