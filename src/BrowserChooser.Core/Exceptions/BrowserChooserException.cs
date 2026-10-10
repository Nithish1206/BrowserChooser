namespace BrowserChooser.Core.Exceptions;

/// <summary>
/// Base type for failures Browser Chooser expects and can explain to the user. Anything else that reaches the top
/// of the application is treated as a bug.
/// </summary>
public class BrowserChooserException : Exception
{
    public BrowserChooserException()
    {
    }

    public BrowserChooserException(string message)
        : base(message)
    {
    }

    public BrowserChooserException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
