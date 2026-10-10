namespace BrowserChooser.Core.Abstractions;

/// <summary>Tells the user about outcomes and errors.</summary>
public interface IUserNotifier
{
    void ShowInformation(string heading, string message);

    void ShowError(string heading, string message, Exception? exception = null);

    /// <summary>Reports a failed launch and offers to pick a different browser.</summary>
    /// <returns><see langword="true"/> if the user wants to choose again.</returns>
    bool AskToChooseAgain(string heading, string message, Exception? exception = null);
}
