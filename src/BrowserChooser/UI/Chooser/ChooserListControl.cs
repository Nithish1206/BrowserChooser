using BrowserChooser.Core.Models;

namespace BrowserChooser.UI.Chooser;

/// <summary>A titled group of rows in the picker, such as "Last used" or "More options".</summary>
internal sealed record ChooserSection(string Header, IReadOnlyList<BrowserTarget> Targets);

/// <summary>
/// The scrollable browser list, drawn like a Windows 11 list: section headers, 32 px icons, a rounded highlight
/// and an accent pill on the selected row. Supports mouse, wheel, keyboard and screen readers.
/// </summary>
internal sealed class ChooserListControl : Control
{
    // Logical (96 DPI) metrics, measured from the system "Select an app" dialog.
    private const int HeaderHeight = 42;
    private const int HeaderTextTop = 14;
    private const int HeaderTextHeight = 20;
    private const int RowHeight = 56;
    private const int RowInsetX = 12;
    private const int RowInsetY = 2;
    private const int RowRadius = 4;
    private const int IconLeft = 24;
    private const int IconSize = 32;
    private const int TextLeft = 68;
    private const int PillWidth = 3;
    private const int PillHeight = 16;
    private const int BottomPadding = 8;
    private const int ScrollThumbWidth = 3;
    private const int ScrollThumbMinHeight = 24;
    private const string ProfileSeparator = "  ·  ";

    private readonly ChooserTheme _theme;
    private readonly BrowserIconProvider _icons;
    private readonly List<Row> _rows = [];
    /// <summary>Indexes into <see cref="_rows"/> of the selectable rows, in display order.</summary>
    private readonly List<int> _entryRows = [];

    private ChooserFonts? _fonts;
    private int _dpi = 96;
    private int _selectedEntry = -1;
    private int _hoveredEntry = -1;
    private int _pressedEntry = -1;
    private int _scrollOffset;

    public ChooserListControl(ChooserTheme theme, BrowserIconProvider icons)
    {
        _theme = theme;
        _icons = icons;
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw | ControlStyles.Selectable,
            true);
        TabStop = true;
        BackColor = theme.Background;
        AccessibleRole = AccessibleRole.List;
        AccessibleName = "Browsers";
    }

    /// <summary>Raised when a row is clicked; the selected row is the one that was clicked.</summary>
    public event EventHandler? EntryActivated;

    public BrowserTarget? SelectedTarget => _selectedEntry >= 0 ? _rows[_entryRows[_selectedEntry]].Target : null;

    /// <summary>Height of all rows in device pixels; the form shows the whole list if it fits.</summary>
    public int ContentHeight { get; private set; }

    public void SetSections(IReadOnlyList<ChooserSection> sections, BrowserTarget? selected)
    {
        ArgumentNullException.ThrowIfNull(sections);

        _rows.Clear();
        _entryRows.Clear();
        foreach (ChooserSection section in sections.Where(section => section.Targets.Count > 0))
        {
            _rows.Add(new Row(section.Header, Target: null));
            foreach (BrowserTarget target in section.Targets)
            {
                _entryRows.Add(_rows.Count);
                _rows.Add(new Row(Header: null, target));
            }
        }

        int index = selected is null ? -1 : _entryRows.FindIndex(row => _rows[row].Target == selected);
        _selectedEntry = index >= 0 ? index : (_entryRows.Count > 0 ? 0 : -1);
        Relayout();
    }

    public void ApplyScale(int dpi, ChooserFonts fonts)
    {
        _dpi = dpi;
        _fonts = fonts;
        Relayout();
    }

    protected override AccessibleObject CreateAccessibilityInstance() => new ListAccessibleObject(this);

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics graphics = e.Graphics;
        graphics.Clear(_theme.Background);
        if (_fonts is null)
            return;

        Painting.PrepareForShapes(graphics);
        for (int index = 0; index < _rows.Count; index++)
        {
            Row row = _rows[index];
            var bounds = new Rectangle(0, row.Top - _scrollOffset, Width, row.Height);
            if (!bounds.IntersectsWith(e.ClipRectangle))
                continue;

            if (row.Target is null)
                PaintHeader(graphics, row.Header!, bounds, _fonts);
            else
                PaintEntry(graphics, row.Target, _entryRows.IndexOf(index), bounds, _fonts);
        }

        PaintScrollThumb(graphics);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        ScrollTo(_scrollOffset);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
        if (_selectedEntry >= 0)
            AccessibilityNotifyClients(AccessibleEvents.Focus, _selectedEntry);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        SetHoveredEntry(EntryAt(e.Location));
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        SetHoveredEntry(-1);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left)
            return;

        Focus();
        int entry = EntryAt(e.Location);
        if (entry < 0)
            return;

        _pressedEntry = entry;
        SelectEntry(entry);
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        int pressed = _pressedEntry;
        if (pressed < 0)
            return;

        _pressedEntry = -1;
        Invalidate();

        // A click opens the link. Like a button, it counts only if released over the row that was pressed, so
        // dragging off the row backs out.
        if (e.Button == MouseButtons.Left && EntryAt(e.Location) == pressed)
            EntryActivated?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        ScrollTo(_scrollOffset - (e.Delta * S(RowHeight) / SystemInformation.MouseWheelScrollDelta));
    }

    protected override bool IsInputKey(Keys keyData) =>
        (keyData & Keys.KeyCode) is Keys.Up or Keys.Down or Keys.Home or Keys.End or Keys.PageUp or Keys.PageDown ||
        base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (_entryRows.Count == 0)
            return;

        int page = Math.Max(1, Height / S(RowHeight));
        int? next = e.KeyCode switch
        {
            Keys.Up => _selectedEntry - 1,
            Keys.Down => _selectedEntry + 1,
            Keys.Home => 0,
            Keys.End => _entryRows.Count - 1,
            Keys.PageUp => _selectedEntry - page,
            Keys.PageDown => _selectedEntry + page,
            _ => null,
        };

        if (next is not null)
        {
            SelectEntry(Math.Clamp(next.Value, 0, _entryRows.Count - 1));
            e.Handled = true;
        }
    }

    private int S(float logical) => Painting.Scale(logical, _dpi);

    private int MaxScrollOffset => Math.Max(0, ContentHeight - Height);

    private void Relayout()
    {
        int top = 0;
        foreach (Row row in _rows)
        {
            row.Top = top;
            row.Height = S(row.Target is null ? HeaderHeight : RowHeight);
            top += row.Height;
        }

        ContentHeight = top + S(BottomPadding);
        ScrollTo(_scrollOffset);
        Invalidate();
    }

    private void PaintHeader(Graphics graphics, string text, Rectangle bounds, ChooserFonts fonts)
    {
        var textBounds = new Rectangle(S(IconLeft), bounds.Top + S(HeaderTextTop), Width - (S(IconLeft) * 2), S(HeaderTextHeight));
        TextRenderer.DrawText(graphics, text, fonts.Body, textBounds, _theme.TextPrimary, _theme.Background, Painting.SingleLine);
    }

    private void PaintEntry(Graphics graphics, BrowserTarget target, int entry, Rectangle bounds, ChooserFonts fonts)
    {
        bool isSelected = entry == _selectedEntry;
        Color background =
            entry == _pressedEntry ? _theme.RowPressed :
            isSelected ? _theme.RowSelected :
            entry == _hoveredEntry ? _theme.RowHover :
            _theme.Background;

        var rowBounds = new RectangleF(
            S(RowInsetX), bounds.Top + S(RowInsetY), Width - (S(RowInsetX) * 2), bounds.Height - (S(RowInsetY) * 2));
        if (background != _theme.Background)
            Painting.FillRoundedRectangle(graphics, background, rowBounds, S(RowRadius));

        if (isSelected)
        {
            float pillHeight = S(PillHeight);
            var pill = new RectangleF(rowBounds.Left, rowBounds.Top + ((rowBounds.Height - pillHeight) / 2), S(PillWidth), pillHeight);
            Painting.FillRoundedRectangle(graphics, _theme.Accent, pill, S(PillWidth) / 2f);

            if (Focused && ShowFocusCues)
                Painting.DrawRoundedRectangle(graphics, _theme.FocusStroke, S(2), RectangleF.Inflate(rowBounds, -S(1), -S(1)), S(RowRadius));
        }

        int iconSize = S(IconSize);
        var iconBounds = new Rectangle(S(IconLeft), bounds.Top + ((bounds.Height - iconSize) / 2), iconSize, iconSize);
        if (_icons.GetIcon(target, iconSize) is { } icon)
            graphics.DrawImage(icon, iconBounds);
        else
            PaintPlaceholderIcon(graphics, target, iconBounds, fonts);

        int textLeft = S(TextLeft);
        var textBounds = new Rectangle(textLeft, bounds.Top, Width - textLeft - S(RowInsetX * 2), bounds.Height);
        TextRenderer.DrawText(graphics, target.Browser.Name, fonts.Body, textBounds, _theme.TextPrimary, background, Painting.SingleLine);

        if (target.Profile is not null)
        {
            int nameWidth = TextRenderer.MeasureText(
                graphics, target.Browser.Name, fonts.Body, textBounds.Size, Painting.SingleLine & ~TextFormatFlags.EndEllipsis).Width;
            var profileBounds = textBounds with { X = textBounds.X + nameWidth, Width = textBounds.Width - nameWidth };
            if (profileBounds.Width > 0)
            {
                TextRenderer.DrawText(
                    graphics, ProfileSeparator + target.Profile.DisplayName, fonts.Body, profileBounds,
                    _theme.TextSecondary, background, Painting.SingleLine);
            }
        }
    }

    /// <summary>For browsers whose icon can't be loaded: the first letter of the name on an accent circle.</summary>
    private void PaintPlaceholderIcon(Graphics graphics, BrowserTarget target, Rectangle bounds, ChooserFonts fonts)
    {
        using (var brush = new SolidBrush(_theme.Accent))
            graphics.FillEllipse(brush, bounds);

        string letter = target.Browser.Name.Length > 0 ? target.Browser.Name[..1].ToUpperInvariant() : "?";
        TextRenderer.DrawText(
            graphics, letter, fonts.Body, bounds, _theme.IsDark ? Color.Black : Color.White, _theme.Accent,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
    }

    private void PaintScrollThumb(Graphics graphics)
    {
        int maxOffset = MaxScrollOffset;
        if (maxOffset <= 0)
            return;

        float margin = S(4);
        float track = Height - (margin * 2);
        float thumbHeight = Math.Max(S(ScrollThumbMinHeight), track * Height / ContentHeight);
        float thumbTop = margin + ((track - thumbHeight) * _scrollOffset / maxOffset);
        float thumbWidth = S(ScrollThumbWidth);
        var thumb = new RectangleF(Width - S(4) - thumbWidth, thumbTop, thumbWidth, thumbHeight);
        Painting.FillRoundedRectangle(graphics, _theme.ScrollThumb, thumb, thumbWidth / 2);
    }

    private int EntryAt(Point location)
    {
        int y = location.Y + _scrollOffset;
        if (location.X < S(RowInsetX) || location.X >= Width - S(RowInsetX))
            return -1;

        for (int entry = 0; entry < _entryRows.Count; entry++)
        {
            Row row = _rows[_entryRows[entry]];
            if (y >= row.Top && y < row.Top + row.Height)
                return entry;
        }

        return -1;
    }

    private Rectangle GetEntryBounds(int entry)
    {
        Row row = _rows[_entryRows[entry]];
        return new Rectangle(S(RowInsetX), row.Top - _scrollOffset + S(RowInsetY), Width - (S(RowInsetX) * 2), row.Height - (S(RowInsetY) * 2));
    }

    private void SetHoveredEntry(int entry)
    {
        if (entry == _hoveredEntry)
            return;

        _hoveredEntry = entry;
        Invalidate();
    }

    private void SelectEntry(int entry)
    {
        if (entry == _selectedEntry)
            return;

        _selectedEntry = entry;
        EnsureVisible(entry);
        Invalidate();

        if (IsHandleCreated)
        {
            AccessibilityNotifyClients(AccessibleEvents.Selection, entry);
            if (Focused)
                AccessibilityNotifyClients(AccessibleEvents.Focus, entry);
        }
    }

    private void EnsureVisible(int entry)
    {
        int rowIndex = _entryRows[entry];
        Row row = _rows[rowIndex];
        // Bring the section header into view along with the first row of its section.
        int top = rowIndex > 0 && _rows[rowIndex - 1].Target is null ? _rows[rowIndex - 1].Top : row.Top;
        int bottom = row.Top + row.Height;

        if (top < _scrollOffset)
            ScrollTo(top);
        else if (bottom > _scrollOffset + Height)
            ScrollTo(bottom - Height + (entry == _entryRows.Count - 1 ? S(BottomPadding) : 0));
    }

    private void ScrollTo(int offset)
    {
        int clamped = Math.Clamp(offset, 0, MaxScrollOffset);
        if (clamped == _scrollOffset)
            return;

        _scrollOffset = clamped;
        Invalidate();
    }

    private sealed class Row(string? Header, BrowserTarget? Target)
    {
        public string? Header { get; } = Header;

        /// <summary><see langword="null"/> for a section header.</summary>
        public BrowserTarget? Target { get; } = Target;

        public int Top { get; set; }

        public int Height { get; set; }
    }

    /// <summary>Exposes each browser row to screen readers as a list item.</summary>
    private sealed class ListAccessibleObject(ChooserListControl owner) : ControlAccessibleObject(owner)
    {
        public override AccessibleRole Role => AccessibleRole.List;

        public override int GetChildCount() => owner._entryRows.Count;

        public override AccessibleObject? GetChild(int index) =>
            index >= 0 && index < owner._entryRows.Count ? new EntryAccessibleObject(owner, index, this) : null;

        public override AccessibleObject? GetFocused() =>
            owner.Focused && owner._selectedEntry >= 0 ? GetChild(owner._selectedEntry) : base.GetFocused();

        public override AccessibleObject? GetSelected() =>
            owner._selectedEntry >= 0 ? GetChild(owner._selectedEntry) : base.GetSelected();
    }

    private sealed class EntryAccessibleObject(ChooserListControl owner, int entry, AccessibleObject parent) : AccessibleObject
    {
        public override string? Name => owner._rows[owner._entryRows[entry]].Target?.DisplayName;

        public override AccessibleRole Role => AccessibleRole.ListItem;

        public override AccessibleObject Parent => parent;

        public override Rectangle Bounds => owner.RectangleToScreen(owner.GetEntryBounds(entry));

        public override string DefaultAction => "Open";

        public override AccessibleStates State
        {
            get
            {
                AccessibleStates state = AccessibleStates.Selectable | AccessibleStates.Focusable;
                if (entry == owner._selectedEntry)
                    state |= AccessibleStates.Selected | (owner.Focused ? AccessibleStates.Focused : AccessibleStates.None);

                return state;
            }
        }

        public override void DoDefaultAction()
        {
            owner.SelectEntry(entry);
            owner.EntryActivated?.Invoke(owner, EventArgs.Empty);
        }

        public override void Select(AccessibleSelection flags)
        {
            if (flags.HasFlag(AccessibleSelection.TakeSelection) || flags.HasFlag(AccessibleSelection.TakeFocus))
                owner.SelectEntry(entry);
        }
    }
}
