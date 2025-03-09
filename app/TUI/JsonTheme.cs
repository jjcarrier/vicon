using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PowerSupplyApp.TUI
{
    public class JsonTheme
    {
        /// <summary>
        /// Defines the theme for interactive mode.
        /// </summary>
        [JsonPropertyName("theme")]
        public string Theme { get; set; } = "classic";

        /// <summary>
        /// The file containing the theme description setting.
        /// </summary>
        private const string themeFilename = "vicon.theme.json";

        /// <summary>
        /// The subdirectory where user settings will be saved.
        /// </summary>
        private const string userSubDir = "jjcarrier/vicon";

        /// <summary>
        /// The subdirectory used when storing to user-space for Windows.
        /// </summary>
        private static string winLocalSettingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), userSubDir);

        /// <summary>
        /// The subdirectory used when storing to user-space for Unix-like systems.
        /// </summary>
        private static string nixLocalSettingsDir = Path.Combine("~/.config", userSubDir);

        /// <summary>
        /// Stores the specified theme name to disk.
        /// Currently there is no error checking for whether the name is valid.
        /// </summary>
        /// <returns>
        /// True if the store to disk succeeded; otherwise false.
        /// </returns>
        public static bool StoreJsonTheme(string themeName)
        {
            JsonTheme theme = new() { Theme = themeName };
            string text = JsonSerializer.Serialize(theme);
            try
            {
                // Try to save in the exe directory
                string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string themeFilePath = Path.Combine(exeDirectory, themeFilename);
                using (StreamWriter outputFile = new(themeFilePath))
                {
                    outputFile.WriteLine(text);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // If access is denied, save in the user-specific folder
                string themeFilePath = (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ?
                    Path.Combine(winLocalSettingsDir, themeFilename) :
                    Path.Combine(nixLocalSettingsDir, themeFilename);
                string? themeFileDir = Path.GetDirectoryName(themeFilePath);
                if (themeFileDir != null)
                {
                    Directory.CreateDirectory(themeFileDir);
                }
                using (StreamWriter outputFile = new(themeFilePath))
                {
                    outputFile.WriteLine(text);
                }
            }
            return true;
        }

        /// <summary>
        /// Loads the theme setting from disk.
        /// </summary>
        /// <returns>The theme to apply to the TUI.</returns>
        public static ColorTheme LoadJsonTheme()
        {
            ColorTheme theme = ColorThemes.Classic;
            string themeFilePath = (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ?
                Path.Combine(winLocalSettingsDir, themeFilename) :
                Path.Combine(nixLocalSettingsDir, themeFilename);

            if (!File.Exists(themeFilePath))
            {
                // If the file is not found in the user-specific folder,
                // try to load from the exe directory
                string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
                themeFilePath = Path.Combine(exeDirectory, themeFilename);
            }

            if (!File.Exists(themeFilePath))
            {
                return theme;
            }

            using (StreamReader r = new StreamReader(themeFilePath))
            {
                try
                {
                    string json = r.ReadToEnd();
                    JsonTheme? loadedTheme = JsonSerializer.Deserialize<JsonTheme>(json, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    switch (loadedTheme?.Theme)
                    {
                        default:
                        case "classic":
                            theme = ColorThemes.Classic;
                            break;

                        case "black-and-white":
                            theme = ColorThemes.BlackAndWhite;
                            break;

                        case "grey":
                            theme = ColorThemes.Grey;
                            break;

                        case "dark-red":
                            theme = ColorThemes.DarkRed;
                            break;

                        case "dark-green":
                            theme = ColorThemes.DarkGreen;
                            break;

                        case "blue":
                            theme = ColorThemes.Blue;
                            break;

                        case "blue-violet":
                            theme = ColorThemes.BlueViolet;
                            break;

                        case "dark-magenta":
                            theme = ColorThemes.DarkMagenta;
                            break;

                        case "cyan":
                            theme = ColorThemes.Cyan;
                            break;

                        case "gold":
                            theme = ColorThemes.Gold;
                            break;
                    }
                }
                catch (Exception)
                {
                    // For now, ignore the error.
                    // In the future JSON schema validation errors should be provided to the user.
                }
            }

            return theme;
        }
    }
}
