using BrowserChooser.Core.Abstractions;
using BrowserChooser.Core.Commands;
using BrowserChooser.Core.Discovery;
using BrowserChooser.Core.Launching;
using BrowserChooser.Core.Preferences;
using BrowserChooser.Core.Profiles;
using BrowserChooser.Core.Registration;
using Microsoft.Extensions.DependencyInjection;

namespace BrowserChooser.Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers discovery, launching, registration and the command handlers. The host must also register
    /// <see cref="IBrowserPicker"/> and <see cref="IUserNotifier"/>, which are UI concerns.
    /// </summary>
    public static IServiceCollection AddBrowserChooserCore(this IServiceCollection services, AppEnvironment environment)
    {
        services.AddOptions();
        services.AddSingleton(environment);

        services.AddSingleton<BrowserClassifier>();
        // Settings first, so a custom entry wins over the registry entry for the same executable.
        services.AddSingleton<IBrowserSource, ConfiguredBrowserSource>();
        services.AddSingleton<IBrowserSource, RegistryBrowserSource>();
        services.AddSingleton<IProfileProvider, ChromiumProfileProvider>();
        services.AddSingleton<IProfileProvider, FirefoxProfileProvider>();
        services.AddSingleton<IBrowserCatalog, BrowserCatalog>();

        services.AddSingleton<IProcessStarter, ProcessStarter>();
        services.AddSingleton<IBrowserLauncher, BrowserLauncher>();
        services.AddSingleton<IAppRegistrar, WindowsAppRegistrar>();
        services.AddSingleton<IPreferencesStore, JsonPreferencesStore>();

        services.AddSingleton<OpenLinkHandler>();
        services.AddSingleton<RegistrationHandler>();
        return services;
    }
}
