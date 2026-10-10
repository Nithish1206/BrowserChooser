namespace BrowserChooser.Core.Commands;

/// <summary>Process exit codes, so scripts (such as the installer) can tell what happened.</summary>
public enum ExitCode
{
    Success = 0,
    Cancelled = 1,
    InvalidArguments = 2,
    NoBrowsersFound = 3,
    LaunchFailed = 4,
    RegistrationFailed = 5,
    UnexpectedError = 99,
}
