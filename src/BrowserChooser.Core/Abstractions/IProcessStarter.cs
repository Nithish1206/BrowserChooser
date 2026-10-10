using System.Diagnostics;

namespace BrowserChooser.Core.Abstractions;

/// <summary>Starts processes. Exists so launching can be tested without starting real browsers.</summary>
public interface IProcessStarter
{
    void Start(ProcessStartInfo startInfo);
}
