using System.ComponentModel;
using System.Diagnostics;
using BrowserChooser.Core.Abstractions;
using BrowserChooser.Core.Exceptions;
using BrowserChooser.Core.Models;
using Microsoft.Extensions.Logging;

namespace BrowserChooser.Core.Launching;

/// <summary>
/// Starts the chosen browser executable directly. It never asks the shell to "open the URL": once Browser
/// Chooser is the default browser, the shell would just start Browser Chooser again.
/// </summary>
internal sealed class BrowserLauncher(IProcessStarter processStarter, ILogger<BrowserLauncher> logger) : IBrowserLauncher
{
    public void Launch(BrowserTarget target, Uri url)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(url);

        string executablePath = target.Browser.ExecutablePath;
        if (!File.Exists(executablePath))
        {
            throw new BrowserLaunchException(target,
                $"{target.Browser.Name} was not found at \"{executablePath}\". It may have been uninstalled or moved.");
        }

        var startInfo = new ProcessStartInfo(executablePath)
        {
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(executablePath) ?? string.Empty,
        };
        foreach (string argument in LaunchArgumentsBuilder.Build(target, url))
            startInfo.ArgumentList.Add(argument);

        try
        {
            processStarter.Start(startInfo);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException)
        {
            throw new BrowserLaunchException(target, $"Windows could not start {target.Browser.Name}.", ex);
        }

        logger.LogInformation("Opened {Link} in {Target}", LinkParser.ToLogString(url), target.DisplayName);
    }
}
