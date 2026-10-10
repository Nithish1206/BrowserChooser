using BrowserChooser.Core.Models;

namespace BrowserChooser.Core.Preferences;

/// <summary>Choices remembered between runs.</summary>
/// <param name="LastUsed">The most recent successful choice; listed first in the picker and preselected.</param>
public sealed record UserPreferences(TargetReference? LastUsed = null)
{
    public static UserPreferences Empty { get; } = new();
}

/// <summary>
/// A saved reference to a <see cref="BrowserTarget"/>. Stores ids rather than paths, so it still matches after a
/// browser updates or moves.
/// </summary>
/// <param name="DisplayName">Only for showing the user what is saved; not used for matching.</param>
public sealed record TargetReference(string BrowserId, string? ProfileId, string DisplayName)
{
    public static TargetReference From(BrowserTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return new TargetReference(target.Browser.Id, target.Profile?.Id, target.DisplayName);
    }

    /// <returns>The matching target, or <see langword="null"/> if that browser or profile no longer exists.</returns>
    public BrowserTarget? FindIn(IEnumerable<BrowserTarget> targets) =>
        targets.FirstOrDefault(target =>
            string.Equals(target.Browser.Id, BrowserId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(target.Profile?.Id, ProfileId, StringComparison.OrdinalIgnoreCase));
}
