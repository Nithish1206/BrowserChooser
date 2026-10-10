namespace BrowserChooser.Core.Models;

/// <summary>Browser engines differ in where profiles are stored and how a profile is chosen on the command line.</summary>
public enum BrowserFamily
{
    /// <summary>Unknown engine: launched with the link only, without profile support.</summary>
    Other = 0,

    /// <summary>Edge, Chrome, Brave, Vivaldi and other Chromium browsers. Profiles are listed in "User Data\Local State".</summary>
    Chromium,

    /// <summary>Mozilla Firefox. Profiles are listed in "%APPDATA%\Mozilla\Firefox\profiles.ini".</summary>
    Firefox,
}
