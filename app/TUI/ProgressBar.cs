using System;
using Spectre.Console;

namespace PowerSupplyApp
{
    /// <summary>
    /// Provides a simple mechanism to generate a progress bar using extended unicode characters.
    /// This feature may not be supported by some terminal emulators.
    /// </summary>
    public static class ProgressBar
    {
        /// <summary>
        /// Gets the Markup instance representing a progress bar based on the provided input parameters.
        /// </summary>
        /// <param name="progress">
        /// A value from 0.0 to 1.0 representing the ratio of the progress bar filled with the foreground
        /// color versus background color from the provided <paramref>style</paramref>.
        /// </param>
        /// <param name="width">The width (in characters) of the progress bar.</param>
        /// <param name="style">The styling to apply to the progress bar.</param>
        /// <returns></returns>
        public static Markup GetMarkup(double progress, int width, Style style)
        {
            string[] fractionalChars = { " ", "▏", "▎", "▍", "▌", "▋", "▊", "▉" };
            progress = Math.Min(1, Math.Max(0, progress));
            double remainderWidth = (progress * width) % 1;
            int wholeChar = (int)Math.Floor(progress * width);
            int factionalCharIndex = (int)Math.Floor(remainderWidth * 8);
            string fractionalChar = fractionalChars[factionalCharIndex];

            if (width - wholeChar - 1 < 0)
            {
                fractionalChar = string.Empty;
            }

            string foregroundChars = new string('█', wholeChar);
            string backgroundChars = new string('█', width - wholeChar - 1);
            string bgColor = style.Background.ToMarkup();
            string fgColor = style.Foreground.ToMarkup();

            return new Markup($"[{fgColor}]{foregroundChars}{fractionalChar}[/][{bgColor}]{backgroundChars}[/]", style);
        }
    }
}
