using BrowserChooser.Core.Abstractions;
using BrowserChooser.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BrowserChooser.UI.Chooser;

/// <summary>Shows <see cref="ChooserForm"/>, the Windows 11 style picker.</summary>
internal sealed class ChooserBrowserPicker(
    IOptions<AppearanceOptions> appearance,
    ILogger<BrowserIconProvider> iconLogger,
    ILogger<ChooserBrowserPicker> logger) : IBrowserPicker
{
    public BrowserTarget? Pick(Uri url, IReadOnlyList<BrowserTarget> targets, BrowserTarget? lastUsed)
    {
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(targets);

        ChooserTheme theme = ChooserTheme.For(appearance.Value.Theme);
        using var icons = new BrowserIconProvider(iconLogger);
        using var form = new ChooserForm(url, BuildSections(targets, lastUsed), lastUsed, theme, icons);
        form.ShowDialog();

        logger.LogDebug("Picker closed with {Choice}", form.Result?.DisplayName ?? "no choice");
        return form.Result;
    }

    /// <summary>Like the system dialog: the remembered choice on top, everything else under "More options".</summary>
    private static List<ChooserSection> BuildSections(IReadOnlyList<BrowserTarget> targets, BrowserTarget? lastUsed) =>
        lastUsed is null
            ? [new ChooserSection("Browsers", targets)]
            : [new ChooserSection("Last used", [lastUsed]), new ChooserSection("More options", targets.Where(target => target != lastUsed).ToList())];
}
