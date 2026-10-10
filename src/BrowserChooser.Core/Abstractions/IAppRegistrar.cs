using BrowserChooser.Core.Exceptions;
using BrowserChooser.Core.Registration;

namespace BrowserChooser.Core.Abstractions;

/// <summary>Registers Browser Chooser with Windows as a browser that can handle http and https links.</summary>
public interface IAppRegistrar
{
    RegistrationStatus GetStatus(string executablePath);

    /// <exception cref="AppRegistrationException">The registration could not be written.</exception>
    void Register(string executablePath);

    /// <exception cref="AppRegistrationException">The registration could not be removed.</exception>
    void Unregister();
}
