using System.Diagnostics;
using BrowserChooser.Core.Abstractions;

namespace BrowserChooser.Core.Launching;

internal sealed class ProcessStarter : IProcessStarter
{
    public void Start(ProcessStartInfo startInfo)
    {
        // Disposing only releases our handle; the started process keeps running.
        using Process? process = Process.Start(startInfo);
    }
}
