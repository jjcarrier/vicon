using Newtonsoft.Json;

namespace PowerSupplyApp.TUI
{
    public class JsonTheme
    {
        /// <summary>
        /// Defines the theme for interactive mode.
        /// </summary>
        [JsonProperty("theme")]
        public string Theme { get; set; } = "classic";

        /// <summary>
        /// The file containing the theme description setting.
        /// </summary>
        private const string themeFilename = "vicon.theme.json";

        /// <summary>
        /// Stores the specified theme name to disk.
        /// Currently there is no error checking for whether the name is valid.
        /// </summary>
        /// <returns>
        /// True if the store to disk succeeded; otherwise false.
        /// </returns>
        public static bool StoreJsonTheme(string themeName)
        {
            JsonTheme theme = new JsonTheme() { Theme = themeName };
            string text = JsonConvert.SerializeObject(theme);
            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string themeFilePath = Path.Combine(exeDirectory, themeFilename);
            using (StreamWriter outputFile = new StreamWriter(themeFilePath))
            {
                outputFile.WriteLine(text);
            }
            return true;
        }

        /// <summary>
        /// Loads the theme setting from disk.
        /// </summary>
        /// <returns>The theme to apply to teh TUI.</returns>
        public static ColorTheme LoadJsonTheme()
        {
            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string themeFilePath = Path.Combine(exeDirectory, themeFilename);
            ColorTheme theme = ColorThemes.Classic;

            if (!File.Exists(themeFilePath))
            {
                return theme;
            }

            using (StreamReader r = new StreamReader(themeFilePath))
            {
                try
                {
                    string json = r.ReadToEnd();
                    JsonTheme loadedTheme = JsonConvert.DeserializeObject<JsonTheme>(json);
                    switch (loadedTheme.Theme)
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
