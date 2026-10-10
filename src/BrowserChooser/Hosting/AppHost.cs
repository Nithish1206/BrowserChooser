using BrowserChooser.Core;
using BrowserChooser.Core.Abstractions;
using BrowserChooser.Core.Configuration;
using BrowserChooser.Core.DependencyInjection;
using BrowserChooser.Logging;
using BrowserChooser.UI;
using BrowserChooser.UI.Chooser;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Extensions.Logging;

namespace BrowserChooser.Hosting;

/// <summary>
/// Composition root: builds configuration, logging and the service container.
/// </summary>
/// <remarks>
/// This deliberately avoids the .NET Generic Host: the process starts on every link click, so start-up time
/// matters more than hosted-service features it would bring.
/// </remarks>
internal sealed class AppHost : IDisposable
{
    private const string SettingsFileName = "appsettings.json";

    private readonly ServiceProvider _serviceProvider;

    private AppHost(ServiceProvider serviceProvider, AppEnvironment environment)
    {
        _serviceProvider = serviceProvider;
        Environment = environment;
        Logger = serviceProvider.GetRequiredService<ILogger<AppHost>>();
    }

    public IServiceProvider Services => _serviceProvider;

    public AppEnvironment Environment { get; }

    public ILogger Logger { get; }

    /// <summary>
    /// Broken settings or an unwritable log folder are reported as warnings and replaced by defaults: neither may
    /// stop a clicked link from opening.
    /// </summary>
    public static AppHost Build()
    {
        AppEnvironment environment = AppEnvironment.FromCurrentProcess();
        var warnings = new List<StartupWarning>();

        IConfiguration configuration = LoadConfiguration(environment, warnings);
        FileLoggingOptions loggingOptions = Bind<FileLoggingOptions>(configuration, FileLoggingOptions.SectionName, warnings);
        BrowserChooserOptions browserOptions = Bind<BrowserChooserOptions>(configuration, BrowserChooserOptions.SectionName, warnings);
        AppearanceOptions appearanceOptions = Bind<AppearanceOptions>(configuration, AppearanceOptions.SectionName, warnings);

        Serilog.ILogger? fileLogger = FileLogging.TryCreateLogger(loggingOptions, environment.LogsDirectory, warnings);

        var services = new ServiceCollection();
        services.AddLogging(logging =>
        {
            // Serilog applies the configured minimum level; let everything through to it.
            logging.SetMinimumLevel(LogLevel.Trace);
            if (fileLogger is not null)
                logging.AddProvider(new SerilogLoggerProvider(fileLogger, dispose: true));
        });
        services.AddSingleton<IOptions<BrowserChooserOptions>>(Options.Create(browserOptions));
        services.AddSingleton<IOptions<AppearanceOptions>>(Options.Create(appearanceOptions));
        services.AddBrowserChooserCore(environment);

        services.AddSingleton<IBrowserPicker, ChooserBrowserPicker>();
        services.AddSingleton<IUserNotifier, TaskDialogUserNotifier>();
        services.AddSingleton<HomeDialog>();
        services.AddSingleton<CommandDispatcher>();

        ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

        var host = new AppHost(provider, environment);
        foreach (StartupWarning warning in warnings)
            host.Logger.LogWarning(warning.Exception, "{StartupWarning}", warning.Message);

        return host;
    }

    public void Dispose() => _serviceProvider.Dispose();

    /// <summary>appsettings.json next to the exe, overridden by an optional per-user file in %LOCALAPPDATA%.</summary>
    private static IConfiguration LoadConfiguration(AppEnvironment environment, List<StartupWarning> warnings)
    {
        try
        {
            return new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(SettingsFileName, optional: true, reloadOnChange: false)
                .AddJsonFile(environment.UserSettingsPath, optional: true, reloadOnChange: false)
                .Build();
        }
        catch (Exception ex) when (ex is InvalidDataException or FormatException or IOException or UnauthorizedAccessException)
        {
            warnings.Add(new StartupWarning("Settings could not be loaded; using defaults", ex));
            return new ConfigurationBuilder().Build();
        }
    }

    private static T Bind<T>(IConfiguration configuration, string sectionName, List<StartupWarning> warnings)
        where T : new()
    {
        var options = new T();
        try
        {
            configuration.GetSection(sectionName).Bind(options);
            return options;
        }
        catch (InvalidOperationException ex)
        {
            // Typically a value that doesn't convert, such as an unknown enum name.
            warnings.Add(new StartupWarning($"The \"{sectionName}\" settings are invalid; using defaults", ex));
            return new T();
        }
    }
}
