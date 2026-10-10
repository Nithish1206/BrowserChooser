using System.Diagnostics;
using BrowserChooser.Core.Abstractions;
using BrowserChooser.Core.Commands;
using BrowserChooser.UI;
using Microsoft.Extensions.Logging;

namespace BrowserChooser.Hosting;

/// <summary>Routes the parsed command line to the handler that does the work.</summary>
internal sealed class CommandDispatcher(
    OpenLinkHandler openLinkHandler,
    RegistrationHandler registrationHandler,
    HomeDialog homeDialog,
    IUserNotifier notifier,
    ILogger<CommandDispatcher> logger)
{
    private const int MaxDisplayedArgumentLength = 200;

    public ExitCode Dispatch(StartupCommand command) => command switch
    {
        StartupCommand.OpenLink openLink => openLinkHandler.Handle(openLink.Url),
        StartupCommand.Register register => registrationHandler.Register(register.OpenSettings),
        StartupCommand.Unregister => registrationHandler.Unregister(),
        StartupCommand.ShowHome => homeDialog.Show(),
        StartupCommand.InvalidLink invalid => RejectInvalidLink(invalid.Value),
        _ => throw new UnreachableException($"Unhandled command {command.GetType().Name}."),
    };

    private ExitCode RejectInvalidLink(string value)
    {
        logger.LogWarning("Rejected a startup argument that is not an http or https link");
        notifier.ShowError(
            "This link can't be opened",
            "Browser Chooser only opens web links (http:// and https://). It was given:\n\n" +
            DisplayText.Shorten(value, MaxDisplayedArgumentLength));
        return ExitCode.InvalidArguments;
    }
}
