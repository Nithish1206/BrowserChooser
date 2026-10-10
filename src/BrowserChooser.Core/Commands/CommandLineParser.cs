namespace BrowserChooser.Core.Commands;

public static class CommandLineParser
{
    public static StartupCommand Parse(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args.Count == 0 || string.IsNullOrWhiteSpace(args[0]))
            return new StartupCommand.ShowHome();

        string first = args[0].Trim();
        if (IsSwitch(first, "register"))
            return new StartupCommand.Register(OpenSettings: !args.Skip(1).Any(arg => IsSwitch(arg.Trim(), "quiet")));
        if (IsSwitch(first, "unregister"))
            return new StartupCommand.Unregister();
        if (IsSwitch(first, "help") || first is "-h" or "/?")
            return new StartupCommand.ShowHome();

        return LinkParser.TryParse(first, out Uri? url)
            ? new StartupCommand.OpenLink(url)
            : new StartupCommand.InvalidLink(first);
    }

    private static bool IsSwitch(string argument, string name) =>
        argument.Equals("--" + name, StringComparison.OrdinalIgnoreCase) ||
        argument.Equals("/" + name, StringComparison.OrdinalIgnoreCase);
}
