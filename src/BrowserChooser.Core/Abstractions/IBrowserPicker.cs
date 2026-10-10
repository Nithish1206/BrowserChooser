using BrowserChooser.Core.Models;

namespace BrowserChooser.Core.Abstractions;

/// <summary>Asks the user which browser and profile should open a link.</summary>
public interface IBrowserPicker
{
    /// <param name="lastUsed">The previous choice, shown first and preselected; <see langword="null"/> if none.</param>
    /// <returns>The chosen target, or <see langword="null"/> if the user cancelled.</returns>
    BrowserTarget? Pick(Uri url, IReadOnlyList<BrowserTarget> targets, BrowserTarget? lastUsed);
}
