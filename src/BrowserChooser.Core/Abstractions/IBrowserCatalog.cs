using BrowserChooser.Core.Models;

namespace BrowserChooser.Core.Abstractions;

/// <summary>Everything the user can choose from: every installed browser, expanded into its profiles.</summary>
public interface IBrowserCatalog
{
    IReadOnlyList<BrowserTarget> GetTargets();
}
