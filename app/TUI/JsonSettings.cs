using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PowerSupplyApp.TUI
{
    public class JsonSettings
    {
        /// <summary>
        /// Defines the polling rate used during interactive mode. Range 0-100 ms.
        /// </summary>
        [JsonPropertyName("poll-rate-ms")]
        [Range(0, 100)]
        public uint PollRate { get; set; } = 1;

        /// <summary>
        /// Defines the theme for interactive mode.
        /// </summary>
        [JsonPropertyName("theme")]
        public string Theme
        {
            get
            {
                return theme.Name;
            }
            set
            {
                theme = ColorThemes.GetTheme(value);
            }
        }

        /// <summary>
        /// The file containing the theme description setting.
        /// </summary>
        private const string settingsFilename = "vicon.settings.json";

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
        /// The loaded theme.
        /// </summary>
        private ColorTheme theme = ColorThemes.Classic;

        /// <summary>
        /// Serialization settings.
        /// </summary>
        private static JsonSerializerOptions serializationOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        /// <summary>
        /// Get the theme found in settings.
        /// </summary>
        /// <returns>The theme.</returns>
        public ColorTheme GetTheme()
        {
            return theme;
        }

        /// <summary>
        /// The file path where settings are stored.
        /// </summary>
        /// <returns></returns>
        public string GetSettingsFilePath()
        {
            string settingsFilePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                Path.Combine(winLocalSettingsDir, settingsFilename) :
                Path.Combine(nixLocalSettingsDir, settingsFilename);

            if (!File.Exists(settingsFilePath))
            {
                // If the file is not found in the user-specific folder,
                // try to load from the exe directory
                string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
                settingsFilePath = Path.Combine(exeDirectory, settingsFilename);
            }

            return settingsFilePath;
        }

        /// <summary>
        /// Stores settings to disk.
        /// </summary>
        /// <returns>
        /// True if the store to disk succeeded; otherwise false.
        /// </returns>
        public bool Store()
        {
            string text = JsonSerializer.Serialize(this, serializationOptions);
            try
            {
                // Try to save in the exe directory
                string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string settingsFilePath = Path.Combine(exeDirectory, settingsFilename);
                using StreamWriter outputFile = new(settingsFilePath);
                outputFile.WriteLine(text);
            }
            catch (UnauthorizedAccessException)
            {
                // If access is denied, save in the user-specific folder
                string settingsFilePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                    Path.Combine(winLocalSettingsDir, settingsFilename) :
                    Path.Combine(nixLocalSettingsDir, settingsFilename);
                string? settingsFileDir = Path.GetDirectoryName(settingsFilePath);
                if (settingsFileDir != null)
                {
                    Directory.CreateDirectory(settingsFileDir);
                }
                using StreamWriter outputFile = new(settingsFilePath);
                outputFile.WriteLine(text);
            }
            return true;
        }

        /// <summary>
        /// Loads the settings from disk.
        /// </summary>
        /// <returns>
        /// True if the load from disk succeeded (or a settings file does not exist); otherwise false.
        /// </returns>
        public bool Load()
        {
            string settingsFilePath = GetSettingsFilePath();

            if (!File.Exists(settingsFilePath))
            {
                return true;
            }

            using StreamReader r = new(settingsFilePath);
            try
            {
                string json = r.ReadToEnd();


                JsonSettings? loadedSettings = JsonSerializer.Deserialize<JsonSettings>(json, serializationOptions);
                if (loadedSettings != null)
                {
                    theme = ColorThemes.GetTheme(loadedSettings.Theme);
                    PollRate = loadedSettings.PollRate;
                    Aliases = loadedSettings.Aliases;
                }
            }
            catch (Exception)
            {
                // In the future, JSON schema validation errors should be provided to the user.
                return false;
            }

            return true;
        }
    }
}
