namespace BrowserChooser.Core.Registration;

public enum RegistrationStatus
{
    NotRegistered,

    /// <summary>Registered, and links are routed to the running executable.</summary>
    Registered,

    /// <summary>Registered, but links are routed to a different copy of Browser Chooser.</summary>
    RegisteredElsewhere,
}
